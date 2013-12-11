<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="RestNetMocker._Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title></title>
</head>
<body>
<h1>This API is being mocked according to the following rules:</h1>
<pre><%Response.Write(System.IO.File.ReadAllText(Server.MapPath("~/.htaccess")));%>
</pre>
</body>
</html>
