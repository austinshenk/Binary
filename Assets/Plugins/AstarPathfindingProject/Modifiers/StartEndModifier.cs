using UnityEngine;
using System.Collections;
using Pathfinding;

[System.Serializable]
/** Adjusts start and end points of a path.
 * \ingroup modifiers
 */
public class StartEndModifier : Modifier {
	
	public override ModifierData input {
		get { return ModifierData.Vector; }
	}
	
	public override ModifierData output {
		get { return (addPoints ? ModifierData.None : ModifierData.StrictVectorPath) | ModifierData.VectorPath; }
	}
	
	/** Add points to the path instead of replacing. */
	public bool addPoints = false;
	public Exactness exactStartPoint = Exactness.Exact;
	public Exactness exactEndPoint = Exactness.Exact;
	
	/** Sets where the start and end points of a path should be placed */
	public enum Exactness {
		Snapped,			/**< The point is snapped to the first/last node in the path*/
		Exact,				/**< The point is set to the exact point which was passed when calling the pathfinding */
		Interpolate			/**< The point is set to the closest point on the line between either the two first point or the two last points */
	}
	
	public bool useRaycasting = false;
	public LayerMask mask = -1;
	
	public bool useGraphRaycasting = false;
	
	/*public override void ApplyOriginal (Path p) {
		
		if (exactStartPoint) {
			pStart = GetClampedPoint (p.path[0].position, p.originalStartPoint, p.path[0]);
			
			if (!addPoints) {
				p.startPoint = pStart;
			}
		}
		
		if (exactEndPoint) {
			pEnd = GetClampedPoint (p.path[p.path.Length-1].position, p.originalEndPoint, p.path[p.path.Length-1]);
			
			if (!addPoints) {
				p.endPoint = pEnd;
			}
		}
	}*/
	
	public override void Apply (Path p, ModifierData source) {
		
		if (p.vectorPath.Length == 0) {
			return;
		} else if (p.vectorPath.Length < 2 && !addPoints) {
			Vector3[] arr = new Vector3[2];
			arr[0] = p.vectorPath[0];
			arr[1] = p.vectorPath[0];
			p.vectorPath = arr;
		}
		
		Vector3 pStart = Vector3.zero,
		pEnd = Vector3.zero;
		
		if (exactStartPoint == Exactness.Exact) {
			pStart = GetClampedPoint (p.path[0].position, p.originalStartPoint, p.path[0]);
		} else if (exactStartPoint == Exactness.Interpolate) {
			pStart = GetClampedPoint (p.path[0].position, p.originalStartPoint, p.path[0]);
			pStart = Mathfx.NearestPointStrict (p.path[0].position,p.path[1>=p.path.Length?0:1].position,pStart);
		} else {
			pStart = p.path[0].position;
		}
		
		if (exactEndPoint == Exactness.Exact) {
			pEnd   = GetClampedPoint (p.path[p.path.Length-1].position, p.originalEndPoint, p.path[p.path.Length-1]);
		} else if (exactEndPoint == Exactness.Interpolate) {
			pEnd   = GetClampedPoint (p.path[p.path.Length-1].position, p.originalEndPoint, p.path[p.path.Length-1]);
			
			pEnd = Mathfx.NearestPointStrict (p.path[p.path.Length-1].position,p.path[p.path.Length-2<0?0:p.path.Length-2].position,pEnd);
		} else {
			pEnd = p.path[p.path.Length-1].position;
		}
		
		if (!addPoints) {
			//p.vectorPath[0] = p.startPoint;
			//p.vectorPath[p.vectorPath.Length-1] = p.endPoint;
			//Debug.DrawLine (p.vectorPath[0],pStart,Color.green);
			//Debug.DrawLine (p.vectorPath[p.vectorPath.Length-1],pEnd,Color.green);
			p.vectorPath[0] = pStart;
			p.vectorPath[p.vectorPath.Length-1] = pEnd;
			
			
		} else {
			
			Vector3[] newPath = new Vector3[p.vectorPath.Length+(exactStartPoint != Exactness.Snapped ? 1 : 0) + (exactEndPoint  != Exactness.Snapped ? 1 : 0)];
			
			if (exactEndPoint != Exactness.Snapped) {
				newPath[0] = pStart;
			}
			
			if (exactEndPoint != Exactness.Snapped) {
				newPath[newPath.Length-1] = pEnd;
			}
			
			int offset = exactStartPoint != Exactness.Snapped ? 1 : 0;
			for (int i=0;i<p.vectorPath.Length;i++) {
				newPath[i+offset] = p.vectorPath[i];
			}
			p.vectorPath = newPath;
		}
	}
	
	public Vector3 GetClampedPoint (Vector3 from, Vector3 to, Node hint) {
		
		//float minDistance = Mathf.Infinity;
		Vector3 minPoint = to;
		
		if (useRaycasting) {
			RaycastHit hit;
			if (Physics.Linecast (from,to,out hit,mask)) {
				minPoint = hit.point;
				//minDistance = hit.distance;
			}
		}
		
		if (useGraphRaycasting && hint != null) {
			
			NavGraph graph = AstarData.GetGraph (hint);
			
			if (graph != null) {
				IRaycastableGraph rayGraph = graph as IRaycastableGraph;
				
				if (rayGraph != null) {
					GraphHitInfo hit;
					
					if (rayGraph.Linecast (from,minPoint, hint, out hit)) {
						
						//if ((hit.point-from).magnitude < minDistance) {
							minPoint = hit.point;
						//}
					}
				}
			}
		}
		
		return minPoint;
	}
	
}
