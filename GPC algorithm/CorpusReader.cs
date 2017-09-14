using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace GPCLearningModel
{
    // This class is used to read from the corpus file
    // and store the set of printed-word
    // spoken word pairs in memory, ready for use.

    class CorpusReader
    {

        public List<string> printedWords = new List<string>();
        public List<string> spokenWords = new List<string>();


        public CorpusReader(Parameters p)
        {
            StreamReader stream = p.fileCorpus.OpenText();
            string txt;

            do
            {
                txt = stream.ReadLine();

                if (txt == null)
                    continue;

                string[] txtArray = txt.Split(new char[] { ' ' });

                printedWords.Add(txtArray[0]);
                spokenWords.Add(txtArray[1]);

            } while (txt != null);

            stream.Close();
        }
    }
}
