using static Music.Globals;
using static Music.Engine;
using static System.Console;
using static Music.Messages;
using static Music.Scale;


namespace Music
{
    public class Menues
    {
    

        public static Note? NewNote()
        { //ввести одну ноту
            bool success;
            do
            {
                success = true;
                string input;
                MessageL(11, enternote());
                if (notation == Notation.eu) MessageL(8, notes_eu());
                else MessageL(8, notes_us());
                                    
                input = ReadLine()!;
                try
                {
                    Note note = new Note(input);
                        return note;
                }
                catch (IncorrectNote e)
                {
                    Message(12, e.Message);
                    success = false;
                }
            } while (!success);
            return null;
        }

        public static void ChooseTimbre()
        {
            if (player != PLAYER.naudio)
            {
                //MessageL(11, "beeper is working...");
                return;
            }

            MessageL(11, chooseTimbre());   
            int choice = EnterChoice(timbres());
            switch(choice)
            {
                default:
                case 0: timbre = TIMBRE.sin; break;
                case 1: timbre = TIMBRE.tri; break;
                case 2: timbre = TIMBRE.sawtooth; break;
                case 3: timbre = TIMBRE.square; break;
            }
        }        

        public static int EnterNum(int max, int min, string msg = "")
        {
            int num;
            MessageL(11, msg);
            do
            {
                bool validInput = int.TryParse(ReadLine(), out num);
                if (validInput)
                {
                    if (num > max)
                        Message(12, less() + max + "\n");
                    if (num < min)
                        Message(12, more() + min + "\n");
                }
                else Message(12, trymore());
            } while (num > max || num < min);

            return num;
        }


        public static bool EnterBool(string msg)
        {

            MessageL(11, msg + yesno());
            bool choice = true;
            string input;
            do
            {
                input = ReadLine();
                switch (input)
                {
                    case "1":
                    case "y":
                    case "Y": choice = true; break;
                    case "2":
                    case "n":
                    case "N": choice = false; break;
                    default: WriteLine(incorrect_value()); break;
                }
            } while (input != "1" && input != "у" && input != "Y" && input != "2" && input != "n" && input != "N");

            return choice;
        }

        public static int EnterChoice(string msg)
        {

            MessageL(11, msg);
            int input;
            bool validInput = false;
            while (true)
            {
                WriteLine(enter() + " " + integer());

                // Перевірка на введення користувачем цілого числа
                validInput = int.TryParse(ReadLine(), out input);

                if (validInput)  break;
                else
                {
                    // Якщо введено не число, просимо користувача ввести ще раз
                    WriteLine(incorrect_value() + ", " + enter() + integer());
                }
            } 

            return input;
        }

        public static void EnterNotes(ref List<string> chords)
        {// введення нот з клавіатури
            EnterNotes();
            ResetColor();
            while (true)
            {
                string temp = ReadLine();
                string[] parts = temp.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (temp is null || temp == "0" || (temp.All(char.IsWhiteSpace))) break;
                foreach (string i in parts)
                {
                    if (key_to_pitch(i) == -100) { WriteLine(errornote); continue; }
                    else chords.Add(i);
                }
            }
        }

        public static void EnterNotes()
        {
            Message(6, enternotes());
            Color(8);
            if (notation == Notation.eu) WriteLine(notes_eu());
            if (notation == Notation.us) WriteLine(notes_us());
            MessageL(7, zeroend());
        }

        public static T EnterMelody<T>(T melody) where T : Scale
        {

            EnterNotes();
            string temp = ReadLine();
            string[] parts = temp.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (temp is null || temp == "0" || (temp.All(char.IsWhiteSpace))) MessageL(11, note_error());
            else
                foreach (string i in parts)
                {
                    try
                    {
                        Note note = new(i);
                        melody.AddNote(note);
                    }
                    catch (IncorrectNote e) { WriteLine(e.Message); }
                };

            return melody;
        }

        public static void ChooseNotation()
        {
            Message(11, choose_notation());
            Message(7, notationtypes());

            string input;
            do
            {
                input = ReadLine();
                switch (input)
                {
                    case "1":
                    case "eu": notation = Notation.eu; break;
                    case "2":
                    case "us": notation = Notation.us; break;
                    default: WriteLine("incorrect choice"); break;
                }
            } while (input != "1" && input != "eu" && input != "2" && input != "us");
        }

        public static void ChooseLanguage()
        {

            Message(11, "\n1 - Українська" + "\t\t2 - English\n");

            string input;
            do
            {
                input = ReadLine();
                switch (input)
                {
                    case "1":
                    case "uk": lng = LNG.uk; break;
                    case "2":
                    case "en": lng = LNG.en; break;
                    default: WriteLine("incorrect choice"); break;
                }
            } while (input != "1" && input != "en" && input != "2" && input != "uk");
        }

        public static void ChoosePlayer()
        {
            Message(11, choose_player());
            Message(7, players());
            string input;
            do
            {
                input = ReadLine();
                switch (input)
                {
                    case "1":
                    case "B":
                    case "b": player = PLAYER.beeper; break;
                    case "2":
                    case "N":
                    case "n": player = PLAYER.naudio; break;
                    case "3":
                    case "M":
                    case "m": player = PLAYER.midiplayer; break;
                    default: WriteLine("incorrect choice"); break;
                }
            } while (player == null);
            Console.WriteLine(player);
        }

       
    }
}
