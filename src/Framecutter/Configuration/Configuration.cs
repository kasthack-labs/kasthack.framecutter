namespace Framecutter.Configuration
{
    public class Configuration
    {
        public TelegramOptions Telegram { get; set; }
        public CuttingOptions Cutting { get; set; }
    }
    public class CuttingOptions
    {
        public string FramePath { get; set; }
        public Rectangle TargetRectangle { get; set; }

        //public SizeMode SizeMode { get; set; }
    }
    public class Rectangle
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
    }
    public enum SizeMode
    {
        Fiil,               //scale for the smaller size to match the target rect & crop
        Fit,                //scale for the larger sized to match the target rect (leaves space)
        Stretch,            //scale ignoring proportions
        Center,             //just center
    }
}
