//#define DEBUG //Draws a ray for each node visited

using UnityEngine;
using System;
using System.Collections.Generic;

namespace Pathfinding {
	
	/** Finds all nodes within a specified distance from the start.
	 This class will search outwards from the start point and find all nodes which it costs less than ConstantPath::maxGScore to reach, this is usually the same as the distance to them multiplied with 100
	 
	 The path can be called like:
	 \code
//Here you create a new path and set how far it should search. Null is for the callback, but the seeker will handle that
ConstantPath cpath = new ConstantPath(transform.position,2000,null);
//Set the seeker to search for the path (where mySeeker is a variable referencing a Seeker component)
mySeeker.StartPath (cpath,myCallbackFunction);
	 \endcode
	 
	 Then when getting the callback, all nodes will be stored in the variable ConstantPath::allNodes (remember that you need to cast it from Path to ConstantPath first to get the variable).
	 \note Due to the nature of the system, there might be duplicates of some nodes in the array.
	 
	 This list will be sorted by G score (cost/distance to reach the node), however only the last duplicate of a node in the list is guaranteed to be sorted in this way.
	 \shadowimage{constantPath.png}
	 
	  \ingroup paths
	  \astarpro
	  
	**/
	public class ConstantPath : Path {
		
		public ConstantPath ()  : base () {}
		
		/** Creates a new ConstantPath starting from the specified point.
		 * \param start 			From where the path will be started from (the closest node to that point will be used)
		 * \param callbackDelegate	Will be called when the path has completed, leave this to null if you use a Seeker to handle calls
		 * Searching will be stopped when a node has a G score (cost to reach it) higher than what's specified as default value in Pathfinding::EndingConditionDistance  */
		public ConstantPath (Vector3 start, OnPathDelegate callbackDelegate) : base (start,Vector3.zero,callbackDelegate) {
			endingCondition = new EndingConditionDistance ();
			hasEndPoint = false;
		}
		
		/** Creates a new ConstantPath starting from the specified point.
		 * \param start 			From where the path will be started from (the closest node to that point will be used)
		 * \param maxGScore			Searching will be stopped when a node has a G score greater than this
		 * \param callbackDelegate	Will be called when the path has completed, leave this to null if you use a Seeker to handle calls
		 * 
		 * Searching will be stopped when a node has a G score (cost to reach it) greater than \a maxGScore */
		public ConstantPath (Vector3 start, int maxGScore, OnPathDelegate callbackDelegate) : base (start,Vector3.zero,callbackDelegate) {
			endingCondition = new EndingConditionDistance (maxGScore);
			hasEndPoint = false;
		}
		
		/** Reset the path to default values.
		 * Clears the #allNodes list.
		 * \note This does not reset the #endingCondition.
		 * 
		 * Also sets #heuristic to Heuristic.None as it is the default value for this path type
		 */
		public override void Reset (Vector3 start, Vector3 end, OnPathDelegate callbackDelegate, bool reset)
		{
			base.Reset (start, end, callbackDelegate, reset);
			heuristic = Heuristic.None;
			allNodes.Clear ();
		}
		
		//public ConstantPath (Vector3 start, Vector3 end, OnPathDelegate callbackDelegate) : base (start,end,callbackDelegate) {}
		
		//Declare some variables
		
		/** Contains all nodes the path found.
		  * \note Due to the nature of the system, there might be duplicates of some nodes in the array.
		  * This list will be sorted by G score (cost/distance to reach the node), however only the last duplicate of a node in the list is guaranteed to be sorted in this way.
	 	  */
		public List<Node> allNodes = new List<Node>();
		
		/** Controls when the path should terminate.
		 * This is set up automatically in the constructor to an instance of the Pathfinding::EndingConditionDistance class with a \a maxGScore is specified in the constructor.
		 * If you want to use another ending condition.
		 * \see Pathfinding::PathEndingCondition for examples
		 */
		public PathEndingCondition endingCondition;
		
		/** Initializes the path. Sets up the open list and adds the first node to it */
		public override void Initialize () {
			//endNode = null;//--Change!
			base.Initialize ();
			allNodes.Add (startNode);
			
			/*
			System.DateTime startTime = System.DateTime.Now;

			

			//Resets the binary heap, don't clear it because that takes an awful lot of time, instead we can just change the numberOfItems in it (which is just an int)
			//Binary heaps are just like a standard array but are always sorted so the node with the lowest F value can be retrieved faster

			open = AstarPath.active.binaryHeap;
			open.numberOfItems = 1;
			
			//This will not 
			if (startNode == endNode) {
				endNode.parent = null;
				endNode.h = 0;
				endNode.g = 0;
				Trace (endNode);
				foundEnd = true;

				//When using multithreading, this signals that another function should call the callback function for this path
				//sendCompleteCall = true;
				return;
			}

			//Adjust the costs for the end node
			//--Commented out the next two lines!
			//endNodeCosts = endNode.InitialOpen (open,hTarget,(Int3)endPoint,this,false);
			//callback += ResetCosts; /* \todo Might interfere with other paths since other paths might be calculated before #callback is called *


			Node.activePath = this;
			startNode.pathID = pathID;
			startNode.parent = null;
			startNode.cost = 0;
			startNode.g = startNode.penalty;
			startNode.UpdateH (hTarget,heuristic,heuristicScale);

			startNode.InitialOpen (open,hTarget,startIntPoint,this,true);
			searchedNodes++;

			//any nodes left to search?
			if (open.numberOfItems <= 1) {
				LogError ("No open points, the start node didn't open any nodes");
				duration += (System.DateTime.Now.Ticks-startTime.Ticks)*0.0001F;
				return;
			}
			
			current = open.Remove ();
			duration += (System.DateTime.Now.Ticks-startTime.Ticks)*0.0001F;*/
		}
		
		public override float CalculateStep (float remainingFrameTime)
		{
			
			System.DateTime startTime = System.DateTime.Now;
			
			System.Int64 maxTicks = (System.Int64)(remainingFrameTime*10000);
			
			int counter = 0;
			
			//Continue to search while there hasn't ocurred an error and the end hasn't been found
			while (!foundEnd && !error) {
				
				//@Performance Just for debug info
				searchedNodes++;
				
//--- Here's the important stuff				
				//Close the current node, if the current node satisfies the ending condition, the path is finished
				if (endingCondition.TargetFound (this,current)) {
					foundEnd = true;
					break;
				}
				
				//Add Node to allNodes
				allNodes.Add (current);
				
				
//--- Here the important stuff ends
				
				//Loop through all walkable neighbours of the node
				current.Open (open, hTarget,this);
				
				//any nodes left to search?
				if (open.numberOfItems <= 1) {
					//For this path type, this is actually a valid end
					foundEnd = true;
					break;
				}
				
				//Select the node with the lowest F score and remove it from the open list
				current = open.Remove ();
				
				//Check for time every 500 nodes
				if (counter > 500) {
					
					//Have we exceded the maxFrameTime, if so we should wait one frame before continuing the search since we don't want the game to lag
					if ((System.DateTime.Now.Ticks-startTime.Ticks) > maxTicks) {//searchedNodesThisFrame > 20000) {
						
						
						float durationThisFrame = (System.DateTime.Now.Ticks-startTime.Ticks)*0.0001F;
						duration += durationThisFrame;
						
						//Return instead of yield'ing, a separate function handles the yield (CalculatePaths)
						return durationThisFrame;
					}
					
					counter = 0;
				}
				
				counter++;
			
			}
			
			if (foundEnd && !error) {
				Trace (endNode);
			}
			
			float durationThisFrame2 = (System.DateTime.Now.Ticks-startTime.Ticks)*0.0001F;
			duration += durationThisFrame2;
			
			//Return instead of yield'ing, a separate function handles the yield (CalculatePaths)
			return durationThisFrame2;
		}
	}
	
	/** Target is found when the path is longer than a specified value.
	 * Actually this is defined as when the current node's G score is >= a specified amount (EndingConditionDistance::maxGScore).\n
	 * The G score is the cost from the start node to the current node, so an area with a higher penalty (weight) will add more to the G score.
	 * However the G score is usually just the shortest distance from the start to the current node.
	 * 
	 * \see Pathfinding::ConstantPath which uses this ending condition
	 */
	public class EndingConditionDistance : PathEndingCondition {
		
		/** Max G score a node may have */
		public int maxGScore = 100;
		
		public EndingConditionDistance () {}
		public EndingConditionDistance (int maxGScore) {
			this.maxGScore = maxGScore;
		}
		
		public override bool TargetFound (Path p, Node node) {
			return node.g >= maxGScore;
		}
	}
}

