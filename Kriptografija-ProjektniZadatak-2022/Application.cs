using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypto
{
    public class Application
    {

        public static void Start()
        {
            var input = "0";
            while ("3" != input)
            {
                System.Console.Clear();
                System.Console.WriteLine("[1] Login \n[2] Sign Up\n[3] Exit\n");
                System.Console.Write("Enter the choice: ");
                input = Console.ReadLine();

                if ("1" == input.Trim())
                {
                    User currentUser = User.LogIn();
                    if (currentUser != null)
                        Quiz.Start(currentUser);

                }
                else if ("2" == input.Trim())
                    User.SignUp();
            }
            Environment.Exit(0);
        }
    }
}
