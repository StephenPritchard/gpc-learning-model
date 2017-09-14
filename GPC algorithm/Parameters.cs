using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace GPCLearningModel
{
    class Parameters
    {
        #region FIELDS

        public int minAbsFrequency;
        public float minRelFrequency;
        public int minContextRuleDominance;
        public string gpcversion;

        //Filenames
        public FileInfo fileParameters = new FileInfo("parameters");
        public FileInfo fileCorpus;
        public FileInfo fileGPCRules;
        public FileInfo fileRulesAndFreqs;
        public DirectoryInfo GPCDirectory;

        #endregion

        #region CONSTRUCTOR

        public Parameters()
        {
            StreamReader stream = fileParameters.OpenText();

            string line;
            string[] splitline;
            do
            {
                line = stream.ReadLine();

                if ((line == null) || (line == "") || (line[0] == '#'))
                    continue;

                splitline = line.Split(new char[] { ' ' });

                if (splitline.Length < 2)
                    continue;

                switch (splitline[0])
                {
                    case "MinAbsoluteFrequency":
                        minAbsFrequency = int.Parse(splitline[1]);
                        break;
                    case "MinRelativeFrequency":
                        minRelFrequency = float.Parse(splitline[1]);
                        break;
                    case "MinContextRuleDominance":
                        minContextRuleDominance = int.Parse(splitline[1]);
                        break;
                    case "TrainingCorpusFilename":
                        fileCorpus = new FileInfo(splitline[1]);
                        break;
                    default:
                        break;
                }
            } while (line != null);

            stream.Close();

            StringBuilder strbldr = new StringBuilder();
            strbldr.Append("_Abs");
            strbldr.Append(minAbsFrequency);
            strbldr.Append("_Rel");
            strbldr.Append(minRelFrequency);
            strbldr.Append("_Dom");
            strbldr.Append(minContextRuleDominance);

            DirectoryInfo dir = new DirectoryInfo(strbldr.ToString());
            int i = 1;
            while (dir.Exists == true)
            {
                strbldr.AppendFormat(" ({0})", i);
                dir = new DirectoryInfo(strbldr.ToString());
                i++;
            }

            dir.Create();
            GPCDirectory = dir;

            string gpcrulefilename = strbldr.ToString() + "/gpcrules";
            string rulefreqsfilename = strbldr.ToString() + "/" + strbldr.ToString();

            fileGPCRules = new FileInfo(gpcrulefilename);
            fileRulesAndFreqs = new FileInfo(rulefreqsfilename);
        }

        #endregion
    }
}
