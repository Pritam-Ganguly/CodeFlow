namespace CodeFlow.Web.Models
{
    public class PaginationViewModel
    {
        public int PageIndex { get; set; }
        public int TotalPages { get; set; } = 1;
        public int PageSize { get; set; }
        public bool HasPrevious => PageIndex > 1;
        public bool HasNext =>  PageIndex < TotalPages;
        public int StartPage => Math.Max(1, PageIndex - 2);
        public int EndPage => Math.Min(TotalPages, PageIndex + 2);
        public string SearchTerm { get; set; } = string.Empty;
        public int SortType { get; set; } = 0;

        public IEnumerable<int> Pages
        {
            get
            {
                if(StartPage > 1)
                {
                    yield return 1;
                    if (StartPage > 2) yield return -1;
                }

                for(int i=StartPage; i<=EndPage; i++)
                {
                    yield return i;
                }

                if(EndPage < TotalPages)
                {
                    if(EndPage < TotalPages - 1) yield return -1;
                    yield return TotalPages;
                }
            }
        }
    }
}
