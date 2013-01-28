using System;
using UnityEngine;
using Pathfinding;

namespace Pathfinding
{
	/** Cleanup operation called when the pathID variable overflows.
	 * The pathID variable is only 16 bit, meaning it can only hold 65536 values, so when it overflows, this path is added to the path queue to zero all path IDs stored in the nodes.\n
	 * If this is not done, then in extreamly rare cases (less than [average amount of nodes searched per path] in 65536^2 (approximately 0.0000004) , I know, it's ridiculously low, but I do not want to take any risks) a path can find a node which has the same pathID as the path but has not actually been searched and mess up the pathfinding (in the worst case it can possibly (not really sure) cause an ininite loop which crashes the application)
	 */
	public class CleanupPath65K : Path {
		
		public CleanupPath65K () {
		}
		
		public override void Prepare () {
			
			error = true;
			callback = null;
			
			Debug.Log ("+++ Performing 65k Cleanup Operation +++\n+++ Zeroing All Path IDs +++");
			if (AstarPath.active.astarData.graphs == null) {
				Debug.LogError ("No Graphs Are Created");
				return;
			}
			
			NavGraph[] graphs = AstarPath.active.astarData.graphs;
			
			for (int g=0;g<graphs.Length;g++) {
				NavGraph graph = graphs[g];
				if (graph.nodes == null) {
					continue;
				}
				
				for (int i=0;i<graph.nodes.Length;i++) {
					graph.nodes[i].pathID = 0;
				}
			}
		}
		
		//Theese two functions should not be called, but just in case they do, I added overrides
		public override void Initialize () {
			error = true;
		}
		
		public override float CalculateStep (float t) {
			error = true;
			return 1000F;
		}
	}
}

