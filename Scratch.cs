#region CombineOptimalCombinations()
		private List<List<ICondition>> CombineOptimalCombinations<ICondition>(List<List<List<ICondition>>> optimalSet)
		{
		    Dictionary<List<ICondition>, int> combinationVotes = new Dictionary<List<ICondition>, int>();
		    Dictionary<List<ICondition>, double> combinationScores = new Dictionary<List<ICondition>, double>();

		    // Voting ensemble
		    foreach (List<List<ICondition>> fold in optimalSet)
		    {
		        foreach (List<ICondition> combination in fold)
		        {
		            if (!combinationVotes.ContainsKey(combination))
		            {
		                combinationVotes[combination] = 0;
		            }
		            combinationVotes[combination]++;
		        }
		    }

		    // Averaging ensemble
		    foreach (List<List<ICondition>> fold in optimalSet)
		    {
		        foreach (List<ICondition> combination in fold)
		        {
		            if (!combinationScores.ContainsKey(combination))
		            {
		                combinationScores[combination] = 0;
		            }
		            combinationScores[combination] += 1.0 / fold.Count;
		        }
		    }

		    // Combine voting and averaging scores
		    Dictionary<List<ICondition>, double> combinedScores = new Dictionary<List<ICondition>, double>();
		    foreach (var combination in combinationVotes.Keys)
		    {
		        double votingScore = (double)combinationVotes[combination] / optimalSet.Count;
		        double averagingScore = combinationScores[combination];
		        double combinedScore = (votingScore + averagingScore) / 2;
		        combinedScores[combination] = combinedScore;
		    }

		    // Select the top combinations based on combined scores
		    List<KeyValuePair<List<ICondition>, double>> sortedCombinations = combinedScores
		        .OrderByDescending(x => x.Value)
		        .ToList();

		    List<List<ICondition>> optimalCombinations = new List<List<ICondition>>();
		    int count = Math.Min(10, sortedCombinations.Count);
		    for (int i = 0; i < count; i++)
		    {
		        optimalCombinations.Add(sortedCombinations[i].Key);
		    }

		    return optimalCombinations;
		}
		#endregion
