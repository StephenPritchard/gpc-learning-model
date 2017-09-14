using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace GPCLearningModel
{
    class Program
    {
        static void Main(string[] args)
        {
            Parameters parameters = new Parameters();

            GPCLearning rules = new GPCLearning(parameters);          // Create new rules instance, to store all rules to be learnt
            CorpusReader trainingCorpus = new CorpusReader(parameters);  // Load the training corpus from file and store in a corpusReader instance

            System.Console.WriteLine("Please enter a version description for this run of the GPC Rule algorithm:");
            parameters.gpcversion = System.Console.ReadLine();
            System.Console.WriteLine();
            System.Console.WriteLine("Working... this may take up to 30secs....");

            /// FIRST PHASE: single-letter rule learning only
            for (int i = 0; i < trainingCorpus.printedWords.Count; i++)
            {
                // One by one, process all the word pairs read from the training corpus.
                rules.ProcessWordPair(trainingCorpus.printedWords[i], trainingCorpus.spokenWords[i], "single");
                System.Console.WriteLine("phase1: {0}", i);
            }

            rules.DeleteAbsFreqs(parameters);
            rules.CreateContextRules(parameters, trainingCorpus);
            rules.ExtrapolateRules();
            

            /// SECOND PHASE: multi-letter rule learning
            for (int i = 0; i < trainingCorpus.printedWords.Count; i++)
            {
                // One by one, process all the word pairs read from the training corpus.
                rules.ProcessWordPair(trainingCorpus.printedWords[i], trainingCorpus.spokenWords[i], "multi");
                System.Console.WriteLine("phase2: {0}", i);
            }

            rules.DeleteAbsFreqs(parameters);
            rules.CreateContextRules(parameters, trainingCorpus);
            rules.ExtrapolateRules();
            rules.Consolidate();

            // Print rules to fileGPCRules
            rules.PrintRules(parameters);
            rules.PrintFreqLog(parameters);
        }
    }
}
