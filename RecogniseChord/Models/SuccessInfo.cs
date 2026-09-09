using System.Reflection.Metadata.Ecma335;
using NuGet.Protocol;

namespace RecogniseChord.Models
{
    public class SuccessInfo
    {
        public int success2s { get; set; }
        public int success3s { get; set; }
        public int success4s { get; set; }
        public int success5s { get; set; }

        public int successinfo =>
            success2s + success3s + success4s + success5s;


        public string Serialize()
        {
            return this.ToJson();
        }
    }

    public class FailInfo
    {
        public int fail2s { get; set; }
        public int fail3s { get; set; }
        public int fail4s { get; set; }
        public int fail5s { get; set; }

        public int failedinfo =>
            fail2s + fail3s + fail4s + fail5s;


        public string Serialize()
        {
           return this.ToJson();
        }

    }
}
