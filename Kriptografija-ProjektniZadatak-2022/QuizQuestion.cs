using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypto
{
    public class QuizQuestion
    {
        public string Question { get; set; }
        public string Answer { get; set; }
        public Boolean MultipleChoice { get; set; } = false;

        private List<string> answers = null;

        public QuizQuestion(string input)
        {
            string[] inputArray = input.Split('#');
            Question = inputArray[0];
            Answer = inputArray[1];
            if (inputArray.Length == 3)
            {
                MultipleChoice = true;
                answers = inputArray[2].Split(',').ToList();
            }
        }

        public override string ToString()
        {
            if (MultipleChoice)
            {
                return Question + "\n" + string.Join(Environment.NewLine, answers.ToArray());
            }
            else
            {
                return Question;
            }
        }

        // poredi odgovore case insensitive
        public bool CheckIfCorrect(String answer)
        {
            return string.Equals(Answer, answer, StringComparison.OrdinalIgnoreCase);
        }
    }
}
