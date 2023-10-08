namespace DevCmdLine.UI
{
    public struct DevCmdSubOption
    {
        public string text;
        public object context;
        public bool isEnd;

        public DevCmdSubOption(string text, object context, bool isEnd)
        {
            this.text = text;
            this.context = context;
            this.isEnd = isEnd;
        }
    }
}