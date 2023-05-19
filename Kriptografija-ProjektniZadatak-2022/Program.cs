

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;

namespace Crypto
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Utility.PrepareEnvironment();
            //Utility.HideQuestionsInImages(); //WARNING - Ovo samo jednom izvršavamo na početku
            Application.Start();
        }
    }
}