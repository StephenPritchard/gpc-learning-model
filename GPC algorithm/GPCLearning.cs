/// VERSION INFO
/// 150125 Version 5.1
/// This version is the same as the PhD version 5.0, but the code has been
/// cleaned up, and some variable names changed to make more sense.
/// Also added the training corpus name as an option in the parameters file,
/// rather than having it hard coded.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;



namespace GPCLearningModel
{
    class GPCLearning
    {

        #region FIELDS AND CONSTRUCTOR

        /// <summary>
        /// FIELDS
        /// </summary>
        private List<GPCRule> gpcRuleList = new List<GPCRule>();
        private FileInfo fileGPCRules;
        private FileInfo fileRuleFreqLog;
        private int printflag = 0; // just determines whether or not to log when i dont learn from a word

        
        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <param name="p"></param>
        public GPCLearning(Parameters p)
        {
            fileGPCRules = p.fileGPCRules;
            fileRuleFreqLog = p.fileRulesAndFreqs;
        }
        #endregion


        /// <summary>
        /// ProcessWordPair
        /// This is the main method called for using a new printed-word--spoken-word pair
        /// to create new GPC rules and/or update the count of existing rules.
        /// Checks for the presence of X, and calls appropriate method.
        /// </summary>
        /// <param name="letterString"></param>
        /// <param name="phonemeString"></param>
        /// <param name="singleOrMulti"></param>
        public void ProcessWordPair(string letterString, string phonemeString, string singleOrMulti)
        {
            // Check if the letter string contains an 'x' or not.

            if (letterString.Contains('x') || letterString.Contains('X'))
            {
                ProcessWithX(letterString, phonemeString);
            }
            else
            {
                ProcessWithoutX(letterString, phonemeString, singleOrMulti);
            }
        }


        /// <summary>
        /// ProcessWithoutX
        /// Called by ProcessWordPair if the letter string does not contain X.
        /// </summary>
        /// <param name="letterString"></param>
        /// <param name="phonemeString"></param>
        /// <param name="singleOrMulti"></param>
        public void ProcessWithoutX(string letterString, string phonemeString, string singleOrMulti)
        {
            // Process the word-pair differently depending on the length of the
            // the letter string relative to the phoneme string.
            // Avoids looking for complex GPCs if the phase is only intended
            // to look for single-letter rules.
            if ((letterString.Length == phonemeString.Length) && ((singleOrMulti == "single") || (singleOrMulti == "singleOrMulti")))
            {
                ProcessOneToOne(letterString, phonemeString);
            }
            else if ((letterString.Length == phonemeString.Length + 1) && ((singleOrMulti == "multi") || (singleOrMulti == "singleOrMulti")))
            {
                ProcessWithDigraph(letterString, phonemeString);
            }
            else if ((letterString.Length == phonemeString.Length + 2) && ((singleOrMulti == "multi") || (singleOrMulti == "singleOrMulti")))
            {
                ProcessWithTrigraph(letterString, phonemeString);
            }
            else if ((letterString.Length == phonemeString.Length + 3) && ((singleOrMulti == "multi") || (singleOrMulti == "singleOrMulti")))
            {
                ProcessWithQuadgraph(letterString, phonemeString);
            }
            else
            {
                if (printflag == 1)
                {
                    StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                    writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                    writer.Close();
                }
            }
        }


        /// <summary>
        /// ProcessWithX
        /// Called by ProcessWordPair if the letter string contains X.
        /// X rule will only be learned if other gpcs in the word are
        /// single-letter rules, and have already been learned.
        /// </summary>
        /// <param name="letterString"></param>
        /// <param name="phonemeString"></param>
        public void ProcessWithX(string letterString, string phonemeString)
        {
            // If letter string is not one less than phoneme string, and there
            // is a letter X in the letter string, don't try and learn from
            // this pair.
            if (letterString.Length != (phonemeString.Length - 1))
            {
                if (printflag == 1)
                {
                    StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                    writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                    writer.Close();
                }
            }
            else
            {
                int existingRule;       // index to indicate location of rule in the List (doesn't exist = -1)
                int droppedPhonemeWinner = 0;
                int letterOffset;

                List<int> residualPhonemes = new List<int>();
                StringBuilder diPhoneme = new StringBuilder();

                int[] scores = new int[phonemeString.Length];

                // initialise scores array
                for (int i = 0; i < phonemeString.Length; i++)
                {
                    scores[i] = 0;
                }

                List<int>[] validRulePositions = new List<int>[phonemeString.Length];

                for (int i = 0; i < phonemeString.Length; i++)
                {
                    validRulePositions[i] = new List<int>();
                }

                GPCRule.RulePosition rulePosition;

                for (int i = 0; i < phonemeString.Length; i++)
                {
                    letterOffset = 0;

                    for (int j = 0; j < phonemeString.Length; j++)
                    {
                        if (j == i) // leave out a phoneme
                        {
                            letterOffset = 1;
                            continue;
                        }

                        // Check the rule position that applies
                        if ((j == 0) || ((j == 1) && (i == 0))) // beginning
                            rulePosition = GPCRule.RulePosition.beginning;
                        else if ((j == phonemeString.Length - 1) ||
                            ((j == phonemeString.Length - 2) && (i == phonemeString.Length - 1))) // end
                            rulePosition = GPCRule.RulePosition.end;
                        else // middle
                            rulePosition = GPCRule.RulePosition.middle;

                        existingRule = CheckForRule(letterString[j - letterOffset].ToString(), phonemeString[j].ToString(), rulePosition);

                        if (existingRule != -1)
                        {
                            scores[i] += 1;
                            validRulePositions[i].Add(j);
                        }
                    }
                }

                for (int i = 0; i < phonemeString.Length; i++)
                {
                    if (scores[i] >= scores[droppedPhonemeWinner])
                        droppedPhonemeWinner = i;
                }

                // If no single-letter rules were found to apply in the word at all,
                // don't try and learn from this word.
                if (scores[droppedPhonemeWinner] == 0) // the highest score is zero
                {
                    if (printflag == 1)
                    {
                        StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                        writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                        writer.Close();
                    }
                    return;
                }

                for (int i = 0; i < phonemeString.Length; i++)
                {
                    if ((i == droppedPhonemeWinner) || (validRulePositions[droppedPhonemeWinner].Contains(i) == false))
                    {
                        residualPhonemes.Add(i);
                    }
                }

                // If there are not two phonemes left over to correspond to X, don't learn from this word pair.
                if (residualPhonemes.Count != 2)
                {
                    if (printflag == 1)
                    {
                        StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                        writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                        writer.Close();
                    }
                    return;
                }
                else
                {
                    residualPhonemes.Sort();

                    // If residual phonemes are adjacent, everything is good.
                    if (residualPhonemes[0] == (residualPhonemes[1] - 1))
                    {
                        diPhoneme.Append(new char[] { phonemeString[residualPhonemes[0]], phonemeString[residualPhonemes[1]] });
                    }
                    else
                    {
                        if (printflag == 1)
                        {
                            StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                            writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                            writer.Close();
                        }
                        return;
                    }

                    letterOffset = 0;
                    for (int i = 0; i < letterString.Length; i++)
                    {
                        if (i == residualPhonemes[0])
                        {

                            // Check the rule position that applies
                            if ((i == 0) && (residualPhonemes[1] == 1)) // beginning
                                rulePosition = GPCRule.RulePosition.beginning;
                            else if (residualPhonemes[1] == (phonemeString.Length - 1)) // end
                                rulePosition = GPCRule.RulePosition.end;
                            else // middle
                                rulePosition = GPCRule.RulePosition.middle;

                            existingRule = CheckForRule(letterString[i].ToString(), diPhoneme.ToString(), rulePosition);

                            if (existingRule == -1)
                            {
                                gpcRuleList.Add(new GPCRule(rulePosition, GPCRule.RuleType.mphon,
                                    letterString[i].ToString(), diPhoneme.ToString(), false, 1.0f, 1,
                                    letterString, phonemeString, i));
                            }
                            else
                            {
                                gpcRuleList[existingRule].gpcCount += 1;
                                gpcRuleList[existingRule].AddWordPair(letterString, phonemeString, i);
                            }
                        }
                        else if (i == residualPhonemes[1])
                        {
                            letterOffset = 1;
                        }

                            // for other letters besides the X in the letter string,
                        else
                        {
                            // Check the rule position that applies
                            if (i == 0) // beginning
                                rulePosition = GPCRule.RulePosition.beginning;
                            else if (i == letterString.Length - 1) // end
                                rulePosition = GPCRule.RulePosition.end;
                            else // middle
                                rulePosition = GPCRule.RulePosition.middle;

                            existingRule = CheckForRule(letterString[i - letterOffset].ToString(), phonemeString[i].ToString(), rulePosition);

                            // move on if GPC hasn't been seen before
                            if (existingRule == -1)
                                gpcRuleList.Add(new GPCRule(rulePosition, GPCRule.RuleType.single,
                                    letterString[i].ToString(), phonemeString[i].ToString(), false, 1.0f, 1,
                                    letterString, phonemeString, i));
                            else
                            {
                                gpcRuleList[existingRule].gpcCount += 1;
                                gpcRuleList[existingRule].AddWordPair(letterString, phonemeString, i);
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// ProcessOneToOne
        /// Quickly identify single-letter GPCs when there are an equal
        /// Number of letters and phonemes.
        /// </summary>
        /// <param name="letterString"></param>
        /// <param name="phonemeString"></param>
        private void ProcessOneToOne(string letterString, string phonemeString)
        {
            int existingRule;       // index to indicate location of rule in the List (doesn't exist = -1)

            GPCRule.RulePosition rulePosition;
            
            for (int i = 0; i < letterString.Length; i++) // iterate through word one grapheme/phoneme at a time
            {
                // Check the rule position that applies
                if (i == 0) // beginning
                    rulePosition = GPCRule.RulePosition.beginning;
                else if (i == letterString.Length - 1) // end
                    rulePosition = GPCRule.RulePosition.end;
                else // middle
                    rulePosition = GPCRule.RulePosition.middle;

                // run method to check for rule, and store result in existingRule variable
                existingRule = CheckForRule(letterString[i].ToString(), phonemeString[i].ToString(), rulePosition);

                if (existingRule == -1) // If the rule hasn't been seen before, create a new rule
                {
                    gpcRuleList.Add(new GPCRule(rulePosition, GPCRule.RuleType.single, letterString[i].ToString(),
                        phonemeString[i].ToString(), false, 1.0f, 1, letterString, phonemeString, i));
                }
                else  // if the rule is already known, simply increment the frequency of the particular position
                {
                    gpcRuleList[existingRule].gpcCount += 1;
                    gpcRuleList[existingRule].AddWordPair(letterString, phonemeString, i);
                }
            }
        }


        /// <summary>
        /// ProcessWithDigraph
        /// Called when there is one more letters than phonemes. 
        /// </summary>
        /// <param name="letterString"></param>
        /// <param name="phonemeString"></param>
        private void ProcessWithDigraph(string letterString, string phonemeString)
        {
            int existingRule;       // index to indicate location of rule in the List (doesn't exist = -1)
            int droppedLetterWinner = 0;
            int phonemeOffset;

            List<int> residualLetters = new List<int>();
            StringBuilder diGraph = new StringBuilder();

            int[] scores = new int[letterString.Length];
            
            // initialise scores array
            for (int i = 0; i < letterString.Length; i++)
            {
                scores[i] = 0;
            }

            // initialise valid rule positions array
            List<int>[] validRulePositions = new List<int>[letterString.Length];

            for (int i = 0; i < letterString.Length; i++)
            {
                validRulePositions[i] = new List<int>();
            }

            GPCRule.RulePosition rulePosition;

            for (int i = 0; i < letterString.Length; i++)
            {
                phonemeOffset = 0;

                for (int j = 0; j < letterString.Length; j++)
                {
                    if (j == i) // leave out a letter
                    {
                        phonemeOffset = 1;
                        continue;
                    }

                    // Check the rule position that applies
                    if ((j == 0) || ((j == 1) && (i == 0))) // beginning
                        rulePosition = GPCRule.RulePosition.beginning;
                    else if ((j == letterString.Length - 1) ||
                              ((j == letterString.Length - 2) && (i == letterString.Length - 1))) // end
                        rulePosition = GPCRule.RulePosition.end;
                    else // middle
                        rulePosition = GPCRule.RulePosition.middle;
                        
                    existingRule = CheckForRule(letterString[j].ToString(), phonemeString[j-phonemeOffset].ToString(), rulePosition);

                    if (existingRule != -1)
                    {
                        scores[i] += 1;
                        validRulePositions[i].Add(j);
                    }
                }
            }

            for (int i = 0; i < letterString.Length; i++)
            {
                if (scores[i] >= scores[droppedLetterWinner])
                    droppedLetterWinner = i;
            }

            if (scores[droppedLetterWinner] == 0) // the highest score is zero
            {
                if (printflag == 1)
                {
                    StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                    writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                    writer.Close();
                }
                return;
            }

            for (int i = 0; i < letterString.Length; i++)
            {
                if ((i == droppedLetterWinner) || (validRulePositions[droppedLetterWinner].Contains(i) == false))
                {
                    residualLetters.Add(i);
                }
            }

            if ((residualLetters.Count > 2) || (residualLetters.Count == 0))
            {
                if (printflag == 1)
                {
                    StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                    writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                    writer.Close();
                }
                return;
            }
            else if (residualLetters.Count == 1)
            {
                if (residualLetters[0] == 0)
                {
                    diGraph.Append(new char[] { letterString[residualLetters[0]], letterString[residualLetters[0]+1] });
                    residualLetters.Add(residualLetters[0]+1);
                }
                else
                {
                    diGraph.Append(new char[] { letterString[residualLetters[0]-1], letterString[residualLetters[0]] });
                    residualLetters.Add(residualLetters[0] - 1);
                    residualLetters.Sort();
                }

                phonemeOffset = 0;
                for (int i = 0; i < letterString.Length; i++)
                {
                    if (i == residualLetters[0])
                    {

                        // Check the rule position that applies
                        if ((i == 0) && (residualLetters[1] == 1)) // beginning
                            rulePosition = GPCRule.RulePosition.beginning;
                        else if (residualLetters[1] == (letterString.Length - 1)) // end
                            rulePosition = GPCRule.RulePosition.end;
                        else // middle
                            rulePosition = GPCRule.RulePosition.middle;

                        existingRule = CheckForRule(diGraph.ToString(), phonemeString[i].ToString(), rulePosition);

                        if (existingRule == -1)
                        {
                            gpcRuleList.Add(new GPCRule(rulePosition, GPCRule.RuleType.two, diGraph.ToString(),
                                phonemeString[i].ToString(), false, 1.0f, 1, letterString, phonemeString, i));
                        }
                        else
                        {
                            gpcRuleList[existingRule].gpcCount += 1;
                            gpcRuleList[existingRule].AddWordPair(letterString, phonemeString, i);
                        }
                    }
                    else if (i == residualLetters[1])
                    {
                        phonemeOffset = 1;
                    }
                }
            }
            // This ELSE applies only if there are 2 residual letters, the default digraph case.
            else
            {
                residualLetters.Sort();

                if (residualLetters[0] == (residualLetters[1] - 1))
                {
                    diGraph.Append(new char[] { letterString[residualLetters[0]], letterString[residualLetters[1]] });
                }
                else
                {
                    diGraph.Append(new char[] { letterString[residualLetters[0]], '.', letterString[residualLetters[1]] });
                }

                phonemeOffset = 0;
                for (int i = 0; i < letterString.Length; i++)
                {
                    if (i == residualLetters[0])
                    {

                        // Check the rule position that applies
                        if (residualLetters[0] == 0) // beginning
                            rulePosition = GPCRule.RulePosition.beginning;
                        else if ((residualLetters[1] == (letterString.Length - 1)) && (residualLetters[0] == (letterString.Length - 2))) // end
                            rulePosition = GPCRule.RulePosition.end;
                        else // middle
                            rulePosition = GPCRule.RulePosition.middle;

                        existingRule = CheckForRule(diGraph.ToString(), phonemeString[i].ToString(), rulePosition);

                        if (existingRule == -1)
                        {
                            gpcRuleList.Add(new GPCRule(rulePosition, GPCRule.RuleType.two, diGraph.ToString(),
                                phonemeString[i].ToString(), false, 1.0f, 1, letterString, phonemeString, i));
                        }
                        else
                        {
                            gpcRuleList[existingRule].gpcCount += 1;
                            gpcRuleList[existingRule].AddWordPair(letterString, phonemeString, i);
                        }
                    }
                    else if (i == residualLetters[1])
                    {
                        phonemeOffset = 1;
                    }
                    else
                    {
                        // ***************
                        // comment out the contents of this 'else' scenario if you dont want
                        // single letter rules to have their frequencies incremented when
                        // digraphs are being learnt.
                        // ***************

                        // Check the rule position that applies
                        //if (i == 0) // beginning
                        //    rulePosition = GPCRule.RulePosition.beginning;
                        //else if (i == letterString.Length - 1) // end
                        //    rulePosition = GPCRule.RulePosition.end;
                        //else // middle
                        //    rulePosition = GPCRule.RulePosition.middle;

                        //existingRule = CheckForRule(letterString[i].ToString(), phonemeString[i - phonemeOffset].ToString(), rulePosition);

                        //if (existingRule == -1)
                        //    continue;
                        //else
                        //{
                        //    ruleFrequencyList[existingRule] += 1;
                        //}
                    }
                }
            }
        }


        // ######## METHOD: ProcessWithTrigraph
        // This method is used to process pairs where there are
        // more letters than phonemes.
        private void ProcessWithTrigraph(string letterString, string phonemeString)
        {
            int existingRule;       // index to indicate location of rule in the List (doesn't exist = -1)
            int[] droppedLetterPairWinner = {0,0};
            int phonemeOffset;

            List<int> residualLetters = new List<int>();
            StringBuilder triGraph = new StringBuilder();

            int[,] scores = new int[letterString.Length, letterString.Length];
            List<int>[,] validRulePositions = new List<int>[letterString.Length, letterString.Length];

            // initialise scores array and validrulepositions array
            for (int i = 0; i < letterString.Length; i++)
            {
                for (int j = 0; j < letterString.Length; j++)
                {
                    scores[i, j] = 0;
                    validRulePositions[i, j] = new List<int>();
                }
            }

            GPCRule.RulePosition rulePosition;

            for (int i = 0; i < letterString.Length; i++)
            {
                for (int j = 0; j < letterString.Length; j++)
                {
                    if (i == j)
                        continue;

                    phonemeOffset = 0;

                    for (int k = 0; k < letterString.Length; k++)
                    {
                        if ((i == k) || (j == k)) // leave out a letter
                        {
                            phonemeOffset += 1;
                            continue;
                        }

                        if ((j == 0) || ((j == 1) && (i == 0))) // beginning
                            rulePosition = GPCRule.RulePosition.beginning;
                        else if ((j == letterString.Length - 1) ||
                              ((j == letterString.Length - 2) && (i == letterString.Length - 1))) // end
                            rulePosition = GPCRule.RulePosition.end;
                        else // middle
                            rulePosition = GPCRule.RulePosition.middle;

                        // Check the rule position that applies
                        if ((k == 0) || ((k == 1) && (i == 0) || (j == 0)) ||
                            ((k == 2) && (i == 0) && (j == 1)) ||
                            ((k == 2) && (i == 1) && (j == 0))) // beginning
                            rulePosition = GPCRule.RulePosition.beginning;
                        else if ((k == letterString.Length - 1) ||
                            ((k == letterString.Length - 2) && (i == letterString.Length - 1) || (j == letterString.Length - 1)) ||
                            ((k == letterString.Length - 3) && (i == letterString.Length - 2) && (j == letterString.Length - 1)) ||
                            ((k == letterString.Length - 3) && (i == letterString.Length - 1) && (j == letterString.Length - 2))) // end
                            rulePosition = GPCRule.RulePosition.end;
                        else // middle
                            rulePosition = GPCRule.RulePosition.middle;

                        existingRule = CheckForRule(letterString[k].ToString(), phonemeString[k - phonemeOffset].ToString(), rulePosition);

                        if (existingRule != -1)
                        {
                            scores[i, j] += 1;
                            validRulePositions[i, j].Add(k);
                        }
                    }
                }
            }

            for (int i = 0; i < letterString.Length; i++)
            {
                for (int j = 0; j < letterString.Length; j++)
                {
                    if (i == j)
                        continue;

                    if (scores[i,j] >= scores[droppedLetterPairWinner[0], droppedLetterPairWinner[1]])
                    {
                        droppedLetterPairWinner[0] = i;
                        droppedLetterPairWinner[1] = j;
                    }
                }
            }

            if (scores[droppedLetterPairWinner[0], droppedLetterPairWinner[1]] == 0) // the highest score is zero
            {
                if (printflag == 1)
                {
                    StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                    writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                    writer.Close();
                }
                return;
            }

            for (int i = 0; i < letterString.Length; i++)
            {
                if ((i == droppedLetterPairWinner[0]) || 
                    (i == droppedLetterPairWinner[1]) ||
                    (validRulePositions[droppedLetterPairWinner[0],droppedLetterPairWinner[1]].Contains(i) == false))
                {
                    residualLetters.Add(i);
                }
            }

            if (residualLetters.Count != 3)
            {
                if (printflag == 1)
                {
                    StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                    writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                    writer.Close();
                }
                return;
            }

            residualLetters.Sort();

            if (residualLetters[0] == (residualLetters[1] - 1))
            {
                triGraph.Append(new char[] { letterString[residualLetters[0]], letterString[residualLetters[1]] });
            }
            else
            {
                if (printflag == 1)
                {
                    StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                    writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                    writer.Close();
                }
                return;
            }

            if (residualLetters[1] == (residualLetters[2] - 1))
            {
                triGraph.Append(new char[] { letterString[residualLetters[2]] });
            }
            else
            {
                if (printflag == 1)
                {
                    StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                    writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                    writer.Close();
                }
                return;
            }


            phonemeOffset = 0;
            for (int i = 0; i < letterString.Length; i++)
            {
                if (i == residualLetters[0])
                {

                    // Check the rule position that applies
                    if ((i == 0) && (residualLetters[1] == 1) && (residualLetters[2] == 2)) // beginning
                        rulePosition = GPCRule.RulePosition.beginning;
                    else if (residualLetters[2] == (letterString.Length - 1)) // end
                        rulePosition = GPCRule.RulePosition.end;
                    else // middle
                        rulePosition = GPCRule.RulePosition.middle;

                    existingRule = CheckForRule(triGraph.ToString(), phonemeString[i].ToString(), rulePosition);

                    if (existingRule == -1)
                    {
                        gpcRuleList.Add(new GPCRule(rulePosition, GPCRule.RuleType.multi, triGraph.ToString(),
                            phonemeString[i].ToString(), false, 1.0f, 1, letterString, phonemeString, i));
                    }
                    else
                    {
                        gpcRuleList[existingRule].gpcCount += 1;
                        gpcRuleList[existingRule].AddWordPair(letterString, phonemeString, i);
                    }
                }
                else if ((i == residualLetters[1]) || (i == residualLetters[2]))
                {
                    phonemeOffset += 1;
                }
                else
                {

                    // ***************
                    // comment out the contents of this 'else' scenario if you dont want
                    // single letter rules to have their frequencies incremented when
                    // digraphs are being learnt.
                    // ***************

                    // Check the rule position that applies
                    //if (i == 0) // beginning
                    //    rulePosition = GPCRule.RulePosition.beginning;
                    //else if (i == letterString.Length - 1) // end
                    //    rulePosition = GPCRule.RulePosition.end;
                    //else // middle
                    //    rulePosition = GPCRule.RulePosition.middle;

                    //existingRule = CheckForRule(letterString[i].ToString(), phonemeString[i - phonemeOffset].ToString(), rulePosition);

                    //if (existingRule == -1)
                    //    continue;
                    //else
                    //{
                    //    ruleFrequencyList[existingRule] += 1;
                    //}
                }
            }
        }


        // ######## METHOD: ProcessWithQuadgraph
        // This method is used to process pairs where there are
        // more letters than phonemes.
        private void ProcessWithQuadgraph(string letterString, string phonemeString)
        {
            int existingRule;       // index to indicate location of rule in the List (doesn't exist = -1)
            int[] droppedLetterTrioWinner = { 0, 0, 0 };
            int phonemeOffset;

            List<int> residualLetters = new List<int>();
            StringBuilder quadGraph = new StringBuilder();

            int[,,] scores = new int[letterString.Length, letterString.Length, letterString.Length];
            List<int>[, ,] validRulePositions = new List<int>[letterString.Length, letterString.Length, letterString.Length];

            // initialise scores array and valid rule positions array
            for (int i = 0; i < letterString.Length; i++)
            {
                for (int j = 0; j < letterString.Length; j++)
                {
                    for (int k = 0; k < letterString.Length; k++)
                    {
                        scores[i, j, k] = 0;
                        validRulePositions[i, j, k] = new List<int>();
                    }
                }
            }

            GPCRule.RulePosition rulePosition;

            for (int i = 0; i < letterString.Length; i++)
            {
                for (int j = 0; j < letterString.Length; j++)
                {
                    if (i == j)
                        continue;

                    for (int k = 0; k < letterString.Length; k++)
                    {
                        if ((i == k) || (j == k))
                        {
                            continue;
                        }
                        phonemeOffset = 0;

                        for (int l = 0; l < letterString.Length; l++)
                        {
                            if ((i == l) || (j == l) || (k == l)) // leave out a letter
                            {
                                phonemeOffset += 1;
                                continue;
                            }

                            if ((k == 0) || ((k == 1) && (i == 0) || (j == 0)) ||
                           ((k == 2) && (i == 0) && (j == 1)) ||
                           ((k == 2) && (i == 1) && (j == 0))) // beginning
                                rulePosition = GPCRule.RulePosition.beginning;
                            else if ((k == letterString.Length - 1) ||
                           ((k == letterString.Length - 2) && (i == letterString.Length - 1) || (j == letterString.Length - 1)) ||
                           ((k == letterString.Length - 3) && (i == letterString.Length - 2) && (j == letterString.Length - 1)) ||
                           ((k == letterString.Length - 3) && (i == letterString.Length - 1) && (j == letterString.Length - 2))) // end
                                rulePosition = GPCRule.RulePosition.end;
                            else // middle
                                rulePosition = GPCRule.RulePosition.middle;

                            // Check the rule position that applies
                            if ((l == 0) ||
                                ((l == 1) && ((i == 0) || (j == 0) || (k == 0))) ||
                                ((l == 2) && ((i == 1) || (j == 1) || (k == 1)) && ((i == 0) || (j == 0) || (k == 0))) ||
                                ((l == 3) && ((i == 2) || (j == 2) || (k == 2)) && ((i == 1) || (j == 1) || (k == 1)) &&
                                        ((i == 0) || (j == 0) || (k == 0)))) // beginning
                                rulePosition = GPCRule.RulePosition.beginning;
                            else if ((l == letterString.Length - 1) ||
                                ((l == letterString.Length - 2) && ((i == letterString.Length - 1) || (j == letterString.Length - 1) || (k == letterString.Length - 1))) ||
                                ((l == letterString.Length - 3) && ((i == letterString.Length - 2) || (j == letterString.Length - 2) || (k == letterString.Length - 2)) &&
                                      ((i == letterString.Length - 1) || (j == letterString.Length - 1) || (k == letterString.Length - 1))) ||
                                ((l == letterString.Length - 4) && ((i == letterString.Length - 3) || (j == letterString.Length - 3) || (k == letterString.Length - 3)) &&
                                      ((i == letterString.Length - 2) || (j == letterString.Length - 2) || (k == letterString.Length - 2)) &&
                                      ((i == letterString.Length - 1) || (j == letterString.Length - 1) || (k == letterString.Length - 1)))) // end
                                rulePosition = GPCRule.RulePosition.end;
                            else // middle
                                rulePosition = GPCRule.RulePosition.middle;

                            existingRule = CheckForRule(letterString[l].ToString(), phonemeString[l - phonemeOffset].ToString(), rulePosition);

                            if (existingRule != -1)
                            {
                                scores[i, j, k] += 1;
                                validRulePositions[i, j, k].Add(l);
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < letterString.Length; i++)
            {
                for (int j = 0; j < letterString.Length; j++)
                {
                    if (i == j)
                        continue;

                    for (int k = 0; k < letterString.Length; k++)
                    {
                        if ((i == k) || (j == k))
                            continue;

                        if (scores[i, j, k] >= scores[droppedLetterTrioWinner[0], droppedLetterTrioWinner[1], droppedLetterTrioWinner[2]])
                        {
                            droppedLetterTrioWinner[0] = i;
                            droppedLetterTrioWinner[1] = j;
                            droppedLetterTrioWinner[2] = k;
                        }
                    }
                }
            }

            if (scores[droppedLetterTrioWinner[0], droppedLetterTrioWinner[1], droppedLetterTrioWinner[2]] == 0) // the highest score is zero
            {
                if (printflag == 1)
                {
                    StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                    writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                    writer.Close();
                }
                return;
            }

            for (int i = 0; i < letterString.Length; i++)
            {
                if ((i == droppedLetterTrioWinner[0]) ||
                    (i == droppedLetterTrioWinner[1]) ||
                    (i == droppedLetterTrioWinner[2]) ||
                    (validRulePositions[droppedLetterTrioWinner[0], droppedLetterTrioWinner[1], droppedLetterTrioWinner[2]].Contains(i) == false))
                {
                    residualLetters.Add(i);
                }
            }

            if (residualLetters.Count != 4)
            {
                if (printflag == 1)
                {
                    StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                    writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                    writer.Close();
                }
                return;
            }

            residualLetters.Sort();

            if (residualLetters[0] == (residualLetters[1] - 1))
            {
                quadGraph.Append(new char[] { letterString[residualLetters[0]], letterString[residualLetters[1]] });
            }
            else
            {
                if (printflag == 1)
                {
                    StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                    writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                    writer.Close();
                }
                return;
            }

            if (residualLetters[1] == (residualLetters[2] - 1))
            {
                quadGraph.Append(new char[] { letterString[residualLetters[2]] });
            }
            else
            {
                if (printflag == 1)
                {
                    StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                    writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                    writer.Close();
                }
                return;
            }

            if (residualLetters[2] == (residualLetters[3] - 1))
            {
                quadGraph.Append(new char[] { letterString[residualLetters[3]] });
            }
            else
            {
                if (printflag == 1)
                {
                    StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);
                    writer.WriteLine("Couldn't learn from this word pair: {0} {1}.\n", letterString, phonemeString);
                    writer.Close();
                }
                return;
            }


            phonemeOffset = 0;
            for (int i = 0; i < letterString.Length; i++)
            {
                if (i == residualLetters[0])
                {

                    // Check the rule position that applies
                    if ((i == 0) &&
                        (residualLetters[1] == 1) &&
                        (residualLetters[2] == 2) &&
                        (residualLetters[3] == 3)) // beginning
                            rulePosition = GPCRule.RulePosition.beginning;
                    else if (residualLetters[3] == (letterString.Length - 1)) // end
                        rulePosition = GPCRule.RulePosition.end;
                    else // middle
                        rulePosition = GPCRule.RulePosition.middle;

                    existingRule = CheckForRule(quadGraph.ToString(), phonemeString[i].ToString(), rulePosition);

                    if (existingRule == -1)
                    {
                        gpcRuleList.Add(new GPCRule(rulePosition, GPCRule.RuleType.multi, quadGraph.ToString(),
                            phonemeString[i].ToString(), false, 1.0f, 1, letterString, phonemeString, i));
                    }
                    else
                    {
                        gpcRuleList[existingRule].gpcCount += 1;
                        gpcRuleList[existingRule].AddWordPair(letterString, phonemeString, i);
                    }
                }
                else if ((i == residualLetters[1]) || (i == residualLetters[2]) || (i == residualLetters[3]))
                {
                    phonemeOffset += 1;
                }
                else
                {
                    // ***************
                    // comment out the contents of this 'else' scenario if you dont want
                    // single letter rules to have their frequencies incremented when
                    // digraphs are being learnt.
                    // ***************

                    // Check the rule position that applies
                    //if (i == 0) // beginning
                    //    rulePosition = GPCRule.RulePosition.beginning;
                    //else if (i == letterString.Length - 1) // end
                    //    rulePosition = GPCRule.RulePosition.end;
                    //else // middle
                    //    rulePosition = GPCRule.RulePosition.middle;

                    //existingRule = CheckForRule(letterString[i].ToString(), phonemeString[i - phonemeOffset].ToString(), rulePosition);

                    //if (existingRule == -1)
                    //    continue;
                    //else
                    //{
                    //    ruleFrequencyList[existingRule] += 1;
                    //}
                }
            }
        }


        // ######## METHOD: CheckForRule
        // Checks whether a particular rule exists, and returns the index of that rule in the
        // arrays if it does. If it doesn't exist, -1 is returned.
        public int CheckForRule(string grapheme, string phoneme, GPCRule.RulePosition r)
        {
            int index = -1; // assume the rule doesn't exist, unless you find it!

            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                if ((gpcRuleList[i].RGrapheme == grapheme) && (gpcRuleList[i].RPhoneme == phoneme) && ((gpcRuleList[i].RPosition == r) || (gpcRuleList[i].RPosition == GPCRule.RulePosition.all)))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        #region RULE CONSOLIDATION METHODS

        /// <summary>
        /// DeleteAbsFreqs
        /// Removes all rules from the GPCRule list with frequencies less than minAbsFrequency
        /// </summary>
        /// <param name="p"></param>
        public void DeleteAbsFreqs(Parameters p)
        {
            List<GPCRule> bufferGPCRuleList = new List<GPCRule>();

            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                if (gpcRuleList[i].gpcCount >= p.minAbsFrequency)
                {
                    bufferGPCRuleList.Add(gpcRuleList[i]);
                }
            }
            gpcRuleList = bufferGPCRuleList;
        }


        /// <summary>
        /// CreateContextRules
        /// Main method for processing the rules list to create context-sensitive
        /// rules. Calls on a range of helper methods, including:
        /// GetSameGraphemeRules, GetMaxCountRule, FindContextRule and DeleteMarkedRules.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="corpus"></param>
        public void CreateContextRules(Parameters p, CorpusReader corpus)
        {

            List<int> rulesExamined = new List<int>();              // list of INDEXES of rules already examined for context rules.
            List<int> rulesForDeletion = new List<int>();           // list of INDEXES of rules marked for deletion due to below-parameter count.
            List<GPCRule> newContextRules = new List<GPCRule>();

            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                //Check if grapheme i has been previously examined
                if (rulesExamined.IndexOf(i) == -1) // if not...
                {
                    List<int> sameGraphemeRules = new List<int>();
                    sameGraphemeRules = GetSameGraphemeRules(gpcRuleList[i]);
                    //sameGraphemeRules is a list of INDEXES of rules all applying to the same grapheme as rule i.

                    // If there is only one rule for this grapheme, no need to make a context rule for it.
                    if (sameGraphemeRules.Count == 1)
                    {
                        rulesExamined.Add(i);  // add the rule's index to rulesExamined
                        // Note: adding sameGraphemeRules[0] would have added
                        // the same index as i.
                        continue;
                    }

                    for (int j = 0; j < sameGraphemeRules.Count; j++)
                    {
                        // add each of the rules applying to this grapheme
                        // to the list of rulesExamined, now that we are
                        // about to examine them.
                        rulesExamined.Add(sameGraphemeRules[j]);
                    }

                    // max is an index that applies to the full listing of GPC rules, not just to the sameGraphemeRules list.
                    // max is the index of the rule applying to the grapheme under current consideration (i) that has
                    // the highest count.
                    int max = GetMaxCountRule(sameGraphemeRules);

                    // Once the highest count rule has been found, check whether any of the other rules applying to the
                    // same grapheme have sufficient absolute count and sufficient relative count. If not, mark them for
                    // deletion. If they do, the look for a context to decide when the apply the rule in place of the
                    // maxCount rule.
                    for (int j = 0; j < sameGraphemeRules.Count; j++)
                    {
                        if (sameGraphemeRules[j] == max)
                            continue;
                        
                        if (((float)gpcRuleList[sameGraphemeRules[j]].gpcCount < p.minAbsFrequency) ||
                           (((float)gpcRuleList[sameGraphemeRules[j]].gpcCount / (float)gpcRuleList[max].gpcCount) < p.minRelFrequency))
                        {
                            rulesForDeletion.Add(sameGraphemeRules[j]);
                        }
                        else
                        {
                            FindContextRule(gpcRuleList[sameGraphemeRules[j]], corpus, p);
                            rulesForDeletion.Add(sameGraphemeRules[j]);
                        }
                    }
                }      
            }
            // Remove any rules marked for deletion from the global list of GPC candidates.
            DeleteMarkedRules(rulesForDeletion);
        }


        /// <summary>
        /// GetSameGraphemeRules
        /// Returns a list of indexes of gpc rules in gpcRuleList applying to
        /// the same grapheme in the same position as the input rule gpcrule.
        /// </summary>
        /// <param name="gpcrule"></param>
        /// <returns></returns>
        private List<int> GetSameGraphemeRules(GPCRule gpcrule)
        {
            List<int> sameGraphemeRules = new List<int>();
            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                if ((gpcRuleList[i].RGrapheme == gpcrule.RGrapheme) && (gpcRuleList[i].RPosition == gpcrule.RPosition))
                    sameGraphemeRules.Add(i);
            }
            return sameGraphemeRules;
        }


        /// <summary>
        /// Return the index of the rule with the highest count in an input list of rules
        /// </summary>
        /// <param name="ruleList"></param>
        /// <returns></returns>
        private int GetMaxCountRule(List<int> ruleList)
        {
            int max = ruleList[0];
            for (int i = 0; i < ruleList.Count; i++)
            {
                if (gpcRuleList[max].gpcCount < gpcRuleList[ruleList[i]].gpcCount)
                    max = ruleList[i];
            }
            return max;
        }

        private void FindContextRule(GPCRule gpcrule, CorpusReader corpus, Parameters p)
        {
            List<char> preChars = new List<char>();
            List<int> preCharCount = new List<int>();
            List<char> postChars = new List<char>();
            List<int> postCharCount = new List<int>();
            int graphemeIndex = -1; // default is to assume no grapheme present.
                                             // -1 is also returned if the word does not have the same number of letters and phonemes.
            GPCRule dominantRule;

            for (int i = 0; i < corpus.printedWords.Count; i++)
            {
                graphemeIndex = ContainsRule(gpcrule, corpus.printedWords[i], corpus.spokenWords[i]);
                if (graphemeIndex == -1)
                    continue;
                else
                {
                    if (gpcrule.RPosition == GPCRule.RulePosition.beginning)
                    {
                        UpdateCharCount(corpus.printedWords[i][gpcrule.RGrapheme.Length], postChars, postCharCount);
                    }
                    else if (gpcrule.RPosition == GPCRule.RulePosition.end)
                    {
                        UpdateCharCount(corpus.printedWords[i][graphemeIndex - 1], preChars, preCharCount);
                    }
                    else
                    {
                        UpdateCharCount(corpus.printedWords[i][graphemeIndex - 1], preChars, preCharCount);
                        UpdateCharCount(corpus.printedWords[i][graphemeIndex + gpcrule.RGrapheme.Length], postChars, postCharCount);
                    }

                }
            }

            dominantRule = GetDominantRule(gpcrule, preChars, preCharCount, postChars, postCharCount, p);
            if (dominantRule == null)
            {
                return;
            }
            else
            {
                gpcRuleList.Add(dominantRule);
            }
        }


        /// <summary>
        /// DeleteMarkedRules
        /// Removes all rules from the GPCRule list that have been listed in deleteList: these are
        /// rules that have been replaced with context-sensitive rules, or deleted if they did not
        /// have a high enough relative frequency to make a context-sensitive rule.
        /// </summary>
        /// <param name="deleteList"></param>
        private void DeleteMarkedRules(List<int> deleteList)
        {
            List<GPCRule> bufferGPCRuleList = new List<GPCRule>();

            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                if (deleteList.IndexOf(i) == -1) // if a rule isn't on the list, keep it.
                {
                    bufferGPCRuleList.Add(gpcRuleList[i]);
                }
            }
            gpcRuleList = bufferGPCRuleList;
        }


        /// <summary>
        /// ContainsRule
        /// Returns the index of the 1st letter in a grapheme if the GPC rule is used in the word
        /// </summary>
        /// <param name="gpcrule"></param>
        /// <param name="printedWord"></param>
        /// <param name="spokenWord"></param>
        /// <returns></returns>
        private int ContainsRule(GPCRule gpcrule, string printedWord, string spokenWord)
        {
            // Don't look for a context rule in pairs where #letters != #phonemes
            if (printedWord.Length != spokenWord.Length)
                return -1;

            if (gpcrule.RGrapheme.Length >= printedWord.Length)
                return -1;

            return gpcrule.UsesRule(printedWord, spokenWord);
        }


        /// <summary>
        /// UpdateChar
        /// Checks whether a char is already in the list of possible contexts. If so, its count is incremented.
        /// If not, the char is added.
        /// </summary>
        /// <param name="character"></param>
        /// <param name="contextChars"></param>
        /// <param name="charCounts"></param>
        private void UpdateCharCount(char character, List<char> contextChars, List<int> charCounts)
        {
            int index = contextChars.IndexOf(character);
            if (index == -1)
            {
                contextChars.Add(character);
                charCounts.Add(1);
            }
            else
            {
                charCounts[index] += 1;
            }
        }


        /// <summary>
        /// Returns the context sensitive GPCrule
        /// </summary>
        /// <param name="gpcrule"></param>
        /// <param name="preChars"></param>
        /// <param name="preCharCount"></param>
        /// <param name="postChars"></param>
        /// <param name="postCharCount"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private GPCRule GetDominantRule(GPCRule gpcrule,
                                                  List<char> preChars, List<int> preCharCount,
                                                  List<char> postChars, List<int> postCharCount,
                                                  Parameters p)
        {
            int[] preMaxs = new int[2] { 0, 0 };
            int[] postMaxs = new int[2] { 0, 0 };
            int maxIndex;
            int maxPreOrPost;
            int maxValue;
            int secondIndex;
            int secondValue;
            int secondPreOrPost;
            
            preMaxs = GetTwoHighest(preCharCount);
            postMaxs = GetTwoHighest(postCharCount);

            if (preMaxs == null)
            {
                if (postMaxs == null)
                    return null;
                else
                {
                    maxIndex = postMaxs[0];
                    maxPreOrPost = 1;
                    maxValue = postCharCount[maxIndex];
                    secondIndex = postMaxs[1];
                    secondValue = postCharCount[secondIndex];
                    secondPreOrPost = 1;
                }
            }
            else if (postMaxs == null)
            {
                maxIndex = preMaxs[0];
                maxPreOrPost = 0;
                maxValue = preCharCount[maxIndex];
                secondIndex = preMaxs[1];
                secondValue = preCharCount[secondIndex];
                secondPreOrPost = 0;
            }
            else if (preCharCount[preMaxs[0]] == postCharCount[postMaxs[0]])
                return null;
            else if (preCharCount[preMaxs[0]] > postCharCount[postMaxs[0]])
            {
                maxIndex = preMaxs[0];
                maxPreOrPost = 0;
                maxValue = preCharCount[maxIndex];
                if (postCharCount[postMaxs[0]] >= preCharCount[preMaxs[1]])
                {
                    secondIndex = postMaxs[0];
                    secondValue = postCharCount[secondIndex];
                    secondPreOrPost = 1;
                }
                else if (preCharCount[preMaxs[1]] >= postCharCount[postMaxs[1]])
                {
                    secondIndex = preMaxs[1];
                    secondValue = preCharCount[secondIndex];
                    secondPreOrPost = 0;
                }
                else
                {
                    secondIndex = postMaxs[1];
                    secondValue = postCharCount[secondIndex];
                    secondPreOrPost = 1;
                }
            }
            else
            {
                maxIndex = postMaxs[0];
                maxPreOrPost = 1;
                maxValue = postCharCount[maxIndex];

                if (preCharCount[preMaxs[0]] >= postCharCount[postMaxs[1]])
                {
                    secondIndex = preMaxs[0];
                    secondValue = preCharCount[secondIndex];
                    secondPreOrPost = 0;
                }
                else if (postCharCount[postMaxs[1]] >= preCharCount[preMaxs[1]])
                {
                    secondIndex = postMaxs[1];
                    secondValue = postCharCount[secondIndex];
                    secondPreOrPost = 1;
                }
                else
                {
                    secondIndex = preMaxs[1];
                    secondValue = preCharCount[secondIndex];
                    secondPreOrPost = 0;
                }
            }
            
            
            if (((float)maxValue / (float)secondValue) >= p.minContextRuleDominance)
            {
                StringBuilder str = new StringBuilder(15);
                if (maxPreOrPost == 0)
                {
                    str.AppendFormat("[{0}]{1}", preChars[maxIndex], gpcrule.RGrapheme);
                    return new GPCRule(gpcrule.RPosition, GPCRule.RuleType.context, str.ToString(), gpcrule.RPhoneme, gpcrule.RProtection, gpcrule.RWeight, maxValue);
                }
                else
                {
                    str.AppendFormat("{0}[{1}]", gpcrule.RGrapheme, postChars[maxIndex]);
                    return new GPCRule(gpcrule.RPosition, GPCRule.RuleType.context, str.ToString(), gpcrule.RPhoneme, gpcrule.RProtection, gpcrule.RWeight, maxValue);
                }
            }
            else if ((maxIndex == secondIndex) && (maxPreOrPost == secondPreOrPost))
            {
                StringBuilder str = new StringBuilder(15);
                if (maxPreOrPost == 0)
                {
                    str.AppendFormat("[{0}]{1}", preChars[maxIndex], gpcrule.RGrapheme);
                    return new GPCRule(gpcrule.RPosition, GPCRule.RuleType.context, str.ToString(), gpcrule.RPhoneme, gpcrule.RProtection, gpcrule.RWeight, maxValue);
                }
                else
                {
                    str.AppendFormat("{0}[{1}]", gpcrule.RGrapheme, postChars[maxIndex]);
                    return new GPCRule(gpcrule.RPosition, GPCRule.RuleType.context, str.ToString(), gpcrule.RPhoneme, gpcrule.RProtection, gpcrule.RWeight, maxValue);
                }
            }
            else
                return null;
        }


        /// <summary>
        /// Returns indexes for the two highest numbers from a list of numbers
        /// </summary>
        /// <param name="lst"></param>
        /// <returns></returns>
        private int[] GetTwoHighest(List<int> lst)
        {
            int maxIndex = 0;
            int secondIndex = 0;

            if (lst.Count == 0)
                return null;

            if (lst.Count == 1)
                return new int[2] { 0, 0 };

            for (int i = 0; i < lst.Count; i++)
            {
                if (lst[i] > lst[maxIndex])
                {
                    secondIndex = maxIndex;
                    maxIndex = i;
                }
                else if ((lst[i] > lst[secondIndex]) || (secondIndex == maxIndex))
                {
                    secondIndex = i;
                }
            }
            return new int[] { maxIndex, secondIndex };
        }


        /// <summary>
        /// ExtrapolateRules
        /// Finds rules that apply to two positions and extrapolates them to the remaining position,
        /// if there is no different rule using the same grapheme that already applies in the 3rd position.
        /// </summary>
        public void ExtrapolateRules()
        {
            // These flags are used to keep track of where a particular rule is seen. If two flags get set for a rule, then
            // the rule will be turned into an "all positions" rule, *if* there is no rule using the same grapheme in the 3rd position.
            // for the flags, if a rule is found, the singleOrMulti is set to the value of the index of the rule. If no rule is found, then
            // the singleOrMulti is left at -1.
            int flagB = -1;
            int flagM = -1;
            int flagE = -1;

            // These flags are used to check if there is another rule using the same grapheme but different phoneme in the 3rd position,
            // for cases where the rule is only seen in two positions. Extrapolation wont occur to the 3rd position if there is another
            // rule with the same grapheme already occupying that position.
            int flagOtherRuleB = -1;
            int flagOtherRuleM = -1;
            int flagOtherRuleE = -1;

            for (int i = 0; i < gpcRuleList.Count; i++)
            {

                if ((gpcRuleList[i].RGrapheme == null) || (gpcRuleList[i].RPosition == GPCRule.RulePosition.all)) continue;

                switch (gpcRuleList[i].RPosition)
                {
                    case GPCRule.RulePosition.beginning:
                        flagB = i;
                        break;
                    case GPCRule.RulePosition.middle:
                        flagM = i;
                        break;
                    case GPCRule.RulePosition.end:
                        flagE = i;
                        break;
                    default:
                        break;
                }

                // for a given rule, search if the same rule occurs in different positions, and
                // set flags accordingly.
                for (int j = 0; j < gpcRuleList.Count; j++)
                {
                    if (j == i) continue;

                    if ((gpcRuleList[j].RGrapheme == gpcRuleList[i].RGrapheme) &&
                        (gpcRuleList[j].RPhoneme == gpcRuleList[i].RPhoneme))
                    {
                        switch (gpcRuleList[j].RPosition)
                        {
                            case GPCRule.RulePosition.beginning:
                                flagB = j;
                                break;
                            case GPCRule.RulePosition.middle:
                                flagM = j;
                                break;
                            case GPCRule.RulePosition.end:
                                flagE = j;
                                break;
                            default: // ie if it happens to be an "All" rule
                                flagB = j;
                                flagM = j;
                                flagE = j;
                                break;
                        }
                    }

                    if ((gpcRuleList[j].RGrapheme == gpcRuleList[i].RGrapheme) &&
                        (gpcRuleList[j].RPhoneme != gpcRuleList[i].RPhoneme))
                    {
                        switch (gpcRuleList[j].RPosition)
                        {
                            case GPCRule.RulePosition.beginning:
                                flagOtherRuleB = j;
                                break;
                            case GPCRule.RulePosition.middle:
                                flagOtherRuleM = j;
                                break;
                            case GPCRule.RulePosition.end:
                                flagOtherRuleE = j;
                                break;
                            default: // ie if it happens to be an "All" rule
                                flagOtherRuleB = j;
                                flagOtherRuleM = j;
                                flagOtherRuleE = j;
                                break;
                        }
                    }
                }

                // By this point in the method, the flags have been set for the particular rule being looked at.
                // The code below will check if two or more flags have been set, and if so,
                // then the rule will be created for all positions. (NB. rules that apply in each position
                // are kept as 3 separate rules until the ConsolidateRules method is called.

                if ((flagB != -1) && (flagM != -1) && (flagE == -1) && (flagOtherRuleE == -1))
                {
                    gpcRuleList.Add(new GPCRule(GPCRule.RulePosition.end, gpcRuleList[i].RType, gpcRuleList[i].RGrapheme,
                        gpcRuleList[i].RPhoneme, gpcRuleList[i].RProtection, gpcRuleList[i].RWeight,
                        (int)((gpcRuleList[flagB].gpcCount + gpcRuleList[flagM].gpcCount) / 2)));
                }
                else if ((flagB != -1) && (flagM == -1) && (flagE != -1) && (flagOtherRuleM == -1))
                {
                    gpcRuleList.Add(new GPCRule(GPCRule.RulePosition.middle, gpcRuleList[i].RType, gpcRuleList[i].RGrapheme,
                        gpcRuleList[i].RPhoneme, gpcRuleList[i].RProtection, gpcRuleList[i].RWeight,
                        Min(gpcRuleList[flagB].gpcCount, gpcRuleList[flagE].gpcCount)));
                }
                else if ((flagB == -1) && (flagM != -1) && (flagE != -1) && (flagOtherRuleB == -1))
                {
                    gpcRuleList.Add(new GPCRule(GPCRule.RulePosition.beginning, gpcRuleList[i].RType, gpcRuleList[i].RGrapheme,
                        gpcRuleList[i].RPhoneme, gpcRuleList[i].RProtection, gpcRuleList[i].RWeight,
                        Min(gpcRuleList[flagM].gpcCount, gpcRuleList[flagE].gpcCount)));
                }

                flagB = -1;
                flagM = -1;
                flagE = -1;
                flagOtherRuleB = -1;
                flagOtherRuleM = -1;
                flagOtherRuleE = -1;
            }
        }


        /// <summary>
        /// Consolidate
        /// Converts rules that apply in all three positions to a single
        /// "all positions" rule, and
        /// re-orders the list of rules so that it is in an order compatible
        /// with the drc-1.2.1 software gpcrules file format, i.e., in the order:
        /// multi, two, mphon, cs, single.
        /// </summary>
        public void Consolidate()
        {
            List<GPCRule> bufferGPCRuleList = new List<GPCRule>();

            // CONVERT all positions rules to a single All rule.
            int flagB = -1;
            int flagM = -1;
            int flagE = -1;
            int flagA = -1;

            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                if (gpcRuleList[i].RGrapheme == null)
                    continue;

                switch (gpcRuleList[i].RPosition)
                {
                    case GPCRule.RulePosition.beginning:
                        flagB = i;
                        break;
                    case GPCRule.RulePosition.middle:
                        flagM = i;
                        break;
                    case GPCRule.RulePosition.end:
                        flagE = i;
                        break;
                    default:
                        flagA = i;
                        break;
                }

                // for a given rule, search if the same rule occurs in different positions, and
                // set flags accordingly.
                for (int j = 0; j < gpcRuleList.Count; j++)
                {
                    if (j == i) continue;

                    if ((gpcRuleList[j].RGrapheme == gpcRuleList[i].RGrapheme) &&
                        (gpcRuleList[j].RPhoneme == gpcRuleList[i].RPhoneme))
                    {
                        switch (gpcRuleList[j].RPosition)
                        {
                            case GPCRule.RulePosition.beginning:
                                // if there are duplicate beginning position rules
                                // at index i and index j, delete the one at j.
                                if (flagB != -1)
                                {
                                    gpcRuleList[j].RGrapheme = null;
                                }
                                flagB = j;
                                break;

                            case GPCRule.RulePosition.middle:
                                // if there are duplicate middle position rules
                                // at index i and index j, delete the one at j.
                                if (flagM != -1)
                                {
                                    gpcRuleList[j].RGrapheme = null;
                                }
                                flagM = j;
                                break;

                            case GPCRule.RulePosition.end:
                                // if there are duplicate end position rules
                                // at index i and index j, delete the one at j.
                                if (flagE != -1)
                                {
                                    gpcRuleList[j].RGrapheme = null;
                                }
                                flagE = j;
                                break;

                            default: // ie if it happens to be an "All" rule
                                // if there are duplicate all position rules
                                // at index i and index j, delete the one at j.
                                if (flagA != -1)
                                {
                                    gpcRuleList[j].RGrapheme = null;
                                }

                                flagA = j;
                                break;
                        }
                    }
                }

                // If there is an indentical rule in each position, and no
                // all positions rule has as yet been created:
                if ((flagB != -1) && (flagM != -1) && (flagE != -1) && (flagA == -1))
                {
                    gpcRuleList[i].RPosition = GPCRule.RulePosition.all;
                    gpcRuleList[i].gpcCount = (int)(gpcRuleList[flagB].gpcCount + gpcRuleList[flagM].gpcCount + gpcRuleList[flagE].gpcCount) / 3;
                    if (flagB != i) gpcRuleList[flagB].RGrapheme = null;
                    if (flagM != i) gpcRuleList[flagM].RGrapheme = null;
                    if (flagE != i) gpcRuleList[flagE].RGrapheme = null;
                }
                // otherwise, if there is an all positions rule already,
                // delete any individual position rules that have been found.
                else if (flagA != -1)
                {
                    if (flagB != -1) gpcRuleList[flagB].RGrapheme = null;
                    if (flagM != -1) gpcRuleList[flagM].RGrapheme = null;
                    if (flagE != -1) gpcRuleList[flagE].RGrapheme = null;
                }

                flagB = -1;
                flagM = -1;
                flagE = -1;
            }

            // The remaining part of the method is to clean up the lists and get rid of any 
            // null entries created when merging rules into "All" rules.
            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                if (gpcRuleList[i].RGrapheme != null)
                {
                    bufferGPCRuleList.Add(new GPCRule(gpcRuleList[i].RPosition, gpcRuleList[i].RType,
                        gpcRuleList[i].RGrapheme, gpcRuleList[i].RPhoneme, gpcRuleList[i].RProtection,
                        gpcRuleList[i].RWeight, gpcRuleList[i].gpcCount));
                }
            }
            gpcRuleList = bufferGPCRuleList;
            bufferGPCRuleList = new List<GPCRule>();


            // RE-ORDERING

            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                if (gpcRuleList[i].RType == GPCRule.RuleType.multi)
                {
                    if (gpcRuleList[i].RGrapheme.Length >= 4)
                        bufferGPCRuleList.Add(gpcRuleList[i]);
                }
            }

            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                if (gpcRuleList[i].RType == GPCRule.RuleType.multi)
                {
                    if (gpcRuleList[i].RGrapheme.Length < 4)
                        bufferGPCRuleList.Add(gpcRuleList[i]);
                }
            }

            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                if (gpcRuleList[i].RType == GPCRule.RuleType.two)
                {
                    bufferGPCRuleList.Add(gpcRuleList[i]);
                }
            }

            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                if (gpcRuleList[i].RType == GPCRule.RuleType.mphon)
                {
                    bufferGPCRuleList.Add(gpcRuleList[i]);
                }
            }

            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                if (gpcRuleList[i].RType == GPCRule.RuleType.context)
                {
                    bufferGPCRuleList.Add(gpcRuleList[i]);
                }
            }

            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                if (gpcRuleList[i].RType == GPCRule.RuleType.single)
                {
                    bufferGPCRuleList.Add(gpcRuleList[i]);
                }
            }

            gpcRuleList = bufferGPCRuleList;
        }


        /// <summary>
        /// Helper method, returns the smallest of two integers.
        /// Used when allocating a count to an extrapolated rule.
        /// It will have the count of the existing rule with lowest count.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private int Min(int a, int b)
        {
            if (a <= b)
                return a;
            else
                return b;
        }

        #endregion


        #region PRINT AND LOG METHODS
        /// <summary>
        /// PrintRules
        /// Prints all rules that have been learned to the log file.
        /// </summary>
        /// <param name="p"></param>
        public void PrintRules(Parameters p)
        {
            StreamWriter writer = new StreamWriter(fileGPCRules.FullName, true);

            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                writer.WriteLine("{0} {1} {2} {3} {4} {5}",
                    gpcRuleList[i].GetPosChar(), gpcRuleList[i].GetTypeString(), gpcRuleList[i].RGrapheme,
                    gpcRuleList[i].RPhoneme, gpcRuleList[i].GetProtectionChar(), (float)gpcRuleList[i].RWeight);
            }

            // Hacked code to add phonotactic rules each time.
            // Comment this out if you don't want to add the phonotactic rules
            writer.WriteLine("A out d[T] t u 1.0");
            writer.WriteLine("A out n[k] N u 1.0");
            writer.WriteLine("A out [pk]d t u 1.0");
            writer.WriteLine("A out [SJ]d t u 1.0");
            writer.WriteLine("A out [s]d t u 1.0");
            writer.WriteLine("A out [t]z s u 1.0");
            writer.WriteLine("A out [Tf]d t u 1.0");
            // End phonotactic hacked code.

            writer.Close();
        }


        /// <summary>
        /// Prints all rules and frequencies that have been learned to the log file.
        /// </summary>
        /// <param name="p"></param>
        public void PrintFreqLog(Parameters p)
        {
            StreamWriter writer = new StreamWriter(fileRuleFreqLog.FullName, true);

            writer.WriteLine("################################");
            writer.WriteLine("Version: {0}", p.gpcversion);
            writer.WriteLine("Minimum absolute rule frequency: {0}", p.minAbsFrequency);
            writer.WriteLine("Minimum relative contextual frequency: {0}", p.minRelFrequency);
            writer.WriteLine("Minimum context rule dominance: {0}", p.minContextRuleDominance);

            writer.WriteLine();
            writer.WriteLine();
            writer.WriteLine();
            writer.WriteLine();

            for (int i = 0; i < gpcRuleList.Count; i++)
            {
                writer.WriteLine("{0} {1} {2} {3} {4}",
                    gpcRuleList[i].GetPosChar(), gpcRuleList[i].GetTypeString(), gpcRuleList[i].RGrapheme,
                    gpcRuleList[i].RPhoneme, gpcRuleList[i].gpcCount);
            }
            writer.Close();
        }

        #endregion
    }
}
