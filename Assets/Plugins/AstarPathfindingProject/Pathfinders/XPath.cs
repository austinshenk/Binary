using UnityEngine;
using System.Collections;

namespace Pathfinding {
	/** Extended Path.
	 * \ingroup paths
	 * This is the same as a standard path but with a lot more customizations possible.
	 * \note With more customizations does make it slower to calculate but not by very much.
	 */
	public class XPath : Path {
	
		public PathEndingCondition endingCondition = new PathEndingCondition ();
		
		public XPath ()  : base () {}
		
		public XPath (Vector3 start, Vector3 end, OnPathDelegate callbackDelegate) : base (start,end,callbackDelegate) {}
			
		public override void Initialize () {
			base.Initialize ();
			
			if (current != null && endingCondition.TargetFound (this,startNode)) {
				foundEnd = true;
				endNode = startNode;
				Trace (endNode);
			}
		}
		
		public override float CalculateStep (float remainingFrameTime) {
			
			System.DateTime startTime = System.DateTime.Now;
			
			System.Int64 maxTicks = (System.Int64)(remainingFrameTime*10000);
			
			int counter = 0;
			
			//Continue to search while there hasn't ocurred an error and the end hasn't been found
			while (!foundEnd && !error) {
				
				//@Performance Just for debug info
				searchedNodes++;
				
				//Close the current node, if the current node satisfies the ending condition, terminate the path
				if (endingCondition.TargetFound (this,current)) {
					foundEnd = true;
					endNode = current;
					break;
				}
				
				/*if (current == null) {
					Debug.LogWarning ("Current is Null");
					return;
				}*/
				
				//Debug.DrawRay ( current.position, Vector3.up*10,Color.cyan);
				
				
				//Loop through all walkable neighbours of the node
				current.Open (open, hTarget,this);
				
				//any nodes left to search?
				if (open.numberOfItems <= 1) {
					LogError ("No open points, whole area searched");
					
					float durationThisFrame = (System.DateTime.Now.Ticks-startTime.Ticks)*0.0001F;
					duration += durationThisFrame;
						
					return durationThisFrame;
				}
				
				//Select the node with the lowest F score and remove it from the open list
				current = open.Remove ();
				
				//Check for time every 500 nodes to save cycles
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
	
	/** Customized ending condition for a path.
This class can be used to implement a custom ending condition for e.g an Pathfinding::XPath.\n
Inherit from this class and override the #TargetFound function to implement you own ending condition logic.\n
\n
For example, you might want to create an Ending Condition which stops when a node is close enough to a given point.\n
Then what you do is that you create your own class, let's call it EndingConditionProximity and override the function TargetFound to specify our own logic.

\code
public class EndingConditionProximity : Pathfinding.PathEndingCondition {
	
	//The maximum distance to the target node before terminating the path
	public float maxDistance = 10;
	
	public override bool TargetFound (Path p, Node node) {
		return ((Vector3)node.position - p.originalEndPoint).sqrMagnitude <= maxDistance*maxDistance;
	}
}
\endcode

One part at a time. We need to cast the node's position to a Vector3 since internally, it is stored as an integer coordinate (Int3).
Then we subtract the Pathfinding::Path::originalEndPoint from it to get their difference. The original end point is always the exact point specified when calling the path.
As a last step we check the squared magnitude (squared distance, it is much faster than the non-squared distance) and check if it is lower or equal to our maxDistance squared.\n
There you have it, it is as simple as that.
Then you simply assign it to the \a endingCondition variable on, for example an XPath which uses the EndingCondition.

\code
EndingConditionProximity ec = new EndingConditionProximity ();
ec.maxDistance = 100; //Or some other value
myXPath.endingCondition = ec;

//Call the path!
mySeeker.StartPath (ec);
\endcode

Where \a mySeeker is a Seeker component, and \a myXPath is an Pathfinding::XPath.\n

\note The above was written without testing. I hope I haven't made any mistakes, if you try it out, and it doesn't seem to work. Please post a comment below.
\version Written for 3.0.8.3

\see Pathfinding::XPath
\see Pathfinding::ConstantPath

*/
	public class PathEndingCondition {
		
		/** Has the ending condition been fulfilled.
		 * \param p The calculating path
		 * \param node The current node.
		 * This is per default the same as asking if \a node == \a p.endNode */
		public virtual bool TargetFound (Path p, Node node) {
			return node == p.endNode;
		}
	}
}