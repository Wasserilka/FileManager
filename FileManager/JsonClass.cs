namespace FileManager
{
    class Init
    {
        private const string tree_path_default = @"c:\";
        private const int page_default = 0;
        private const string info_default = "";
        private const bool error_default = false;
        public string TreePath { get; set; }
        public int Page { get; set; }
        public string Info { get; set; }
        public bool Error { get; set; }
        public Init(string tree_path, int page, string info, bool error)
        {
            TreePath = tree_path;
            Page = page;
            Info = info;
            Error = error;
        }
        public Init()
        {
            TreePath = tree_path_default;
            Page = page_default;
            Info = info_default;
            Error = error_default;
        }
    }
    class Config
    {
        private const int limit_default = 10;
        private const int length_default = 100;
        private const char symbol_default = '-';
        public int Limit { get; set; }
        public int SplitLineLength { get; set; }
        public char SplitLineChar { get; set; }
        public Config(int limit, int length)
        {
            Limit = limit;
            SplitLineLength = length;
            SplitLineChar = symbol_default;
        }
        public Config()
        {
            Limit = limit_default;
            SplitLineLength = length_default;
            SplitLineChar = symbol_default;
        }
    }
}
