using static System.Console;
using static Music.Menues;
using static Music.Messages;


// картинка з привітанням

namespace Music
{
    public class Welcome
    {
        private string? title;

        private string? description;

        public Welcome() { }

        public Welcome(string title)
        {
            OutputEncoding = System.Text.Encoding.UTF8;            
            this.title = title;
        }
        public Welcome(string title, string description) 
        {
            OutputEncoding = System.Text.Encoding.UTF8;
            this.description = description;
            this.title = title;
        }

        public string Title
        { get => title; set { title = value; } }

        public string Description
        { get => description; set { description = value; } }
        private void Clef()
        {
            ForegroundColor = ConsoleColor.Yellow;
            WriteLine();
            WriteLine("         #");
            WriteLine("         ##");
            WriteLine("         # #");
            WriteLine("         ##");
            WriteLine("        ##");
            WriteLine("       # #");
            WriteLine("      #  #");
            WriteLine("     #   #");
            WriteLine("    #    #");
            WriteLine("   #   # # # *");
            WriteLine("   #  *  #    #");
            WriteLine("    #    #    #");
            WriteLine("      #  #   # ");
            WriteLine("       * * *  ");
            WriteLine("         # ");
            WriteLine("      #  # ");
            WriteLine("       ## ");
            ResetColor();
        }

        public void Show()
        {
            WriteLine("+++++++++++++++++++++++++++++");
            WriteLine(title);
            WriteLine("+++++++++++++++++++++++++++++");
            Clef();
            WriteLine(".............................");
            WriteLine(Description);
            WriteLine(".............................");
            ChooseLanguage();
            Clear();
            ChooseNotation();
            Clear();
            ChoosePlayer();
            Clear();
            ChooseTimbre();
            Clear();
            //Play(1000, 400);
            Note note = new Note("c");            
            Message(8, checkSounds());
            ReadKey();
            Clear();
        }
    }
}

