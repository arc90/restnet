using System;
using System.Collections.Generic;
using System.Text;

namespace RestNet
{
    public class PagingCriteria
    {
        public int Page { get; set; }
        public int ItemsPerPage { get; set; }
        public int TotalItems { get; set; }

        public PagingCriteria(int page, int itemsPerPage, int totalItems)
        {
            Page = page;
            ItemsPerPage = itemsPerPage;
            TotalItems = totalItems;
        }
    }
}
