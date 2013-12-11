using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Principal;
using System.Runtime.InteropServices;

namespace RestNet
{
    public class FastPrincipal : System.Security.Principal.WindowsPrincipal
    {
         #region /********IMPORTS***********/
        //only works on Unicode systems so we ar safe to go with an Auto CharSet
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private extern static bool LookupAccountName(string lpSystemName,
                                                       string lpAccountName,
                                                       IntPtr Sid, ref int cbSid,
                                                       IntPtr ReferencedDomainName, ref int cbReferencedDomainName,
                                                       out int peUse);

        [DllImport("advapi32.dll")]
        public extern static bool CheckTokenMembership(IntPtr TokenHandle, IntPtr SidToCheck, out bool IsMember);


        [DllImport("advapi32.dll")]
        public extern static bool DuplicateToken(IntPtr ExistingTokenHandle,
                                                    int SECURITY_IMPERSONATION_LEVEL,
                                                    ref IntPtr DuplicateTokenHandle);

        [DllImport("kernel32.dll")]
        public extern static bool CloseHandle(IntPtr Handle);

        #endregion

       private const int ERROR_INSUFFICIENT_BUFFER = 122;  //from winerror.h
        private const int SecurityImpersonation = 2;        //SECURITY_IMPERSONATION_LEVEL enum from winnt.h
        private static SidCache s_sidCache = null;
        private static object s_cacheLock = new object();

        private class SidCache : System.Collections.Hashtable
        {
            public SidCache()
                : base()
            {  //ensure we clean up our native heap memory
                System.AppDomain.CurrentDomain.DomainUnload += new EventHandler(DomainUnload);
            }

            private void DomainUnload(Object sender, EventArgs e)
            {
                this.Clear();
            }

            public override void Clear()
            {
                foreach (Sid sid in base.Values)
                {
                    sid.Dispose();
                }
                base.Clear();
            }
        }

        private class Sid : IDisposable
        {
            IntPtr _sidvalue;
            int _length;

            public Sid(IntPtr sid, int length)
            {
                _sidvalue = sid;
                _length = length;
            }

            public int Length { get { return _length; } }

            public IntPtr Value { get { return _sidvalue; } }

            public Sid Copy()
            {
                Sid newSid = new Sid(IntPtr.Zero, 0);
                byte[] buffer = new byte[this.Length];
                newSid._sidvalue = Marshal.AllocHGlobal(this.Length);
                newSid._length = this.Length;
                Marshal.Copy(this.Value, buffer, 0, this.Length);
                Marshal.Copy(buffer, 0, newSid._sidvalue, this.Length);

                return newSid;
            }

            public void Dispose()
            {
                if (_sidvalue != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_sidvalue);
                    _sidvalue = IntPtr.Zero;
                }
                GC.SuppressFinalize(this);
            }
        }//Sid

        public FastPrincipal(WindowsIdentity ntIdentity) : base(ntIdentity) { }


        public override bool IsInRole(string role)
        {
            Sid sid = null;
            bool ret = false;
            try
            {
                sid = GetSid(role);
                if (sid != null && sid.Value != IntPtr.Zero)
                {
                    ret = IsSidInToken(((WindowsIdentity)base.Identity).Token, sid);
                }
            }
            catch {/*Don't allow exceptions to bubble back up*/}
            finally
            {
                if (sid != null)
                {
                    sid.Dispose();
                }
            }

            return ret;
        }

        private bool IsSidInToken(IntPtr token, Sid sid)
        {
            IntPtr impersonationToken = IntPtr.Zero;
            bool inToken = false;

            try
            {
                if (DuplicateToken(token, SecurityImpersonation, ref impersonationToken))
                {
                    CheckTokenMembership(impersonationToken, sid.Value, out inToken);
                }
            }
            finally
            {
                if (impersonationToken != IntPtr.Zero)
                {
                    CloseHandle(impersonationToken);
                }
            }

            return inToken;
        }

        private Sid GetSid(string role)
        {
            Sid sid = null;

            sid = GetSidFromCache(role);
            if (sid == null)
            {
                sid = ResolveNameToSid(role);
                if (sid != null)
                {
                    AddSidToCache(role, sid);
                }
            }
            return sid;
        }

        private Sid GetSidFromCache(string role)
        {
            Sid sid = null;

            if (s_sidCache != null)
            {
                lock (s_cacheLock)
                {
                    Sid cachedsid = (Sid)s_sidCache[role.ToUpper()];
                    if (cachedsid != null)
                    {
                        sid = cachedsid.Copy();
                    }
                }
            }
            return sid;
        }

        private void AddSidToCache(string role, Sid sid)
        {
            if (s_sidCache == null)
            {
                lock (s_cacheLock)
                {
                    if (s_sidCache == null)
                    {
                        s_sidCache = new SidCache();
                    }
                }
            }

            lock (s_cacheLock)
            {
                if (!s_sidCache.Contains(role))
                {
                    s_sidCache.Add(role.ToUpper(), sid.Copy());
                }
            }
        }


        private Sid ResolveNameToSid(string name)
        {
            bool ret = false;
            Sid sid = null;
            IntPtr psid = IntPtr.Zero;
            IntPtr domain = IntPtr.Zero;
            int sidLength = 0;
            int domainLength = 0;
            int sidType = 0;

            try
            {
                ret = LookupAccountName(null,
                                          name,
                                          psid,
                                          ref sidLength,
                                          domain,
                                          ref domainLength,
                                          out sidType);


                if (ret == false &&
                    Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
                {
                    psid = Marshal.AllocHGlobal(sidLength);

                    //LookupAccountName only works on Unicode systems so to ensure
                    //we allocate a LPWSTR
                    domain = Marshal.AllocHGlobal(domainLength * 2);

                    ret = LookupAccountName(null,
                                              name,
                                              psid,
                                              ref sidLength,
                                              domain,
                                              ref domainLength,
                                              out sidType);



                }

                if (ret == true)
                {
                    sid = new Sid(psid, sidLength);
                }

            }
            finally
            {
                if (domain != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(domain);
                }
            }

            return sid;

        }
    }//FastPrincipal
}
