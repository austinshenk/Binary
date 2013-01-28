//#define NoVirtualUpdateH //Should UpdateH be virtual or not
//#define NoVirtualUpdateG //Should UpdateG be virtual or not
//#define NoVirtualOpen //Should Open be virtual or not

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pathfinding {
	[System.Serializable]
	/** Generates a grid of nodes.
	 * \ingroup graphs
	 */
	public class GridGraph : NavGraph, ISerializableGraph, IUpdatableGraph, IFunnelGraph
	{
		
		public override Node[] CreateNodes (int number) {
			
			/*if (nodes != null && graphNodes != null && nodes.Length == number && graphNodes.Length == number) {
				Debug.Log ("Caching");
				return nodes;
			}*/
			
			GridNode[] tmp = new GridNode[number];
			for (int i=0;i<number;i++) {
				tmp[i] = new GridNode ();
			}
			return tmp as Node[];
		}
		
		/** This function will be called when this graph is destroyed */
		public override void OnDestroy () {
			base.OnDestroy ();
			//Clean up a reference in a static variable which otherwise should point to this graph forever and stop the GC from collecting it
			RemoveGridGraphFromStatic ();
			
			//GridNode.RemoveGridGraph (this);
			
			//Just to make sure, clean up the node arrays too (this.nodes is cleaned up in base.OnDestroy ())
			graphNodes = null;
		}
		
		public void RemoveGridGraphFromStatic () {
			GridNode.RemoveGridGraph (this);
		}
		
		/** This is placed here so generators inheriting from this one can override it and set it to false.
		If it is true, it means that the nodes array's length will always be equal to width*depth
		It is used mainly in the editor to do auto-scanning calls, setting it to false for a non-uniform grid will reduce the number of scans */
		public virtual bool uniformWidhtDepthGrid {
			get {
				return true;
			}
		}
		
		public int width; /**< Width of the grid in nodes */
		public int depth; /**< Depth (height) of the grid in nodes */
		
		public float aspectRatio = 1F; /**< Scaling of the graph along the X axis. This should be used if you want different scales on the X and Y axis of the grid */
		//public Vector3 offset;
		public Vector3 rotation; /**< Rotation of the grid in degrees */
		
		public Bounds bounds;
		
		public Vector3 center; /**< Center point of the grid */
		public Vector2 unclampedSize; 	/**< Size of the grid. Might be negative or smaller than #nodeSize */
		public Vector2 size;			/**< Size of the grid. Will always be positive and larger than #nodeSize. \see #GenerateMatrix */
		
		public float nodeSize = 1; /**< Size of one node in world units */
		
		/* Collision and stuff */
		
		/** Settings on how to check for walkability and height */
		public GraphCollision collision;
		
		/** The max position difference between two nodes to enable a connection. Set to 0 to ignore the value.*/
		public float maxClimb = 0;
		
		/** The axis to use for #maxClimb. X = 0, Y = 1, Z = 2. */
		public int maxClimbAxis = 1;
		
		/** The max slope in degrees for a node to be walkable. */
		public float maxSlope = 90;
		
		/** Use heigh raycasting normal for max slope calculation. If this is false, the normal will be calculated based on the nearby nodes in the grid. */
		public bool useRaycastNormal = true;
		
		public int erodeIterations = 0;
		
		/** Auto link the graph's edge nodes together with other GridGraphs in the scene on Scan. \see #autoLinkDistLimit */
		public bool autoLinkGrids = false;
		
		/** Distance limit for grid graphs to be auto linked. \see #autoLinkGrids */
		public float autoLinkDistLimit = 10F;
		
		/* End collision and stuff */
		
		/** Index offset to get neighbour nodes. Added to a node's index to get a neighbour node index */
		[System.NonSerialized]
		public int[] neighbourOffsets;
		
		/** Costs to neighbour nodes */
		[System.NonSerialized]
		public int[] neighbourCosts;
		
		/** Offsets in the X direction for neighbour nodes. Only 1, 0 or -1 */
		[System.NonSerialized]
		public int[] neighbourXOffsets;
		
		/** Offsets in the Z direction for neighbour nodes. Only 1, 0 or -1 */
		[System.NonSerialized]
		public int[] neighbourZOffsets;
		
		/** Same as #nodes, but already in the correct type (GridNode instead of Node) */
		public GridNode[] graphNodes;
		
		/** Number of neighbours for each node. Either four directional or eight directional */
		public NumNeighbours neighbours = NumNeighbours.Eight;
		
		/** If disabled, will not cut corners on obstacles. If \link #neighbours connections \endlink is Eight, obstacle corners might be cut by a connection, setting this to false disables that. \image html images/cutCorners.png */
		public bool cutCorners = true;
		
		/** If a walkable node wasn't found, then it will search (max) in a square with the side of 2*getNearestForceLimit+1 for a close walkable node */
		public int getNearestForceLimit = 40;
		public int getNearestForceOverlap = 2;
		
		public bool penaltyPosition = false;
		public float penaltyPositionOffset = 0;
		public float penaltyPositionFactor = 1F;
		
		public bool penaltyAngle = false;
		public float penaltyAngleFactor = 100F;
		
		
		public Matrix4x4 boundsMatrix;
		public Matrix4x4 boundsMatrix2;
		public Matrix4x4 inverseMatrix;
		
		public int scanns = 0;
		
		
		public GridGraph () {
			unclampedSize = new Vector2 (10,10);
			nodeSize = 1F;
			collision = new GraphCollision ();
		}
		
		/** Updates #size from #width, #depth and #nodeSize values. Also \link GenerateMatrix generates a new matrix \endlink.
		 * \note This does not rescan the graph, that must be done with Scan */
		public void UpdateSizeFromWidthDepth () {
			unclampedSize = new Vector2 (width,depth)*nodeSize;
			GenerateMatrix ();
		}
		
		/** Generates the matrix used for translating nodes from grid coordinates to world coordintes. */
		public void GenerateMatrix () {
			
			size = unclampedSize;
			
			size.x *= Mathf.Sign (size.x);
			//size.y *= Mathf.Sign (size.y);
			size.y *= Mathf.Sign (size.y);
			
			//Clamp the nodeSize at 0.1
			nodeSize = Mathf.Clamp (nodeSize,size.x/1024F,Mathf.Infinity);//nodeSize < 0.1F ? 0.1F : nodeSize;
			nodeSize = Mathf.Clamp (nodeSize,size.y/1024F,Mathf.Infinity);
			
			size.x = size.x < nodeSize ? nodeSize : size.x;
			//size.y = size.y < 0.1F ? 0.1F : size.y;
			size.y = size.y < nodeSize ? nodeSize : size.y;
			
			
			boundsMatrix.SetTRS (center,Quaternion.Euler (rotation),new Vector3 (aspectRatio,1,1));
			
			//bounds.center = boundsMatrix.MultiplyPoint (Vector3.up*height*0.5F);
			//bounds.size = new Vector3 (width*nodeSize,height,depth*nodeSize);
			
			width = Mathf.FloorToInt (size.x / nodeSize);
			depth = Mathf.FloorToInt (size.y / nodeSize);
			
			if (Mathf.Approximately (size.x / nodeSize,Mathf.CeilToInt (size.x / nodeSize))) {
				width = Mathf.CeilToInt (size.x / nodeSize);
			}
			
			if (Mathf.Approximately (size.y / nodeSize,Mathf.CeilToInt (size.y / nodeSize))) {
				depth = Mathf.CeilToInt (size.y / nodeSize);
			}
			
			//height = size.y;
			
			matrix.SetTRS (boundsMatrix.MultiplyPoint3x4 (-new Vector3 (size.x,0,size.y)*0.5F),Quaternion.Euler(rotation), new Vector3 (nodeSize*aspectRatio,1,nodeSize));
			           
			           
			inverseMatrix = matrix.inverse;
		}
		
		//public void GenerateBounds () {
			//bounds.center = offset+new Vector3 (0,height*0.5F,0);
			//bounds.size = new Vector3 (width*scale,height,depth*scale);
		//}
		
		public override NNInfo GetNearest (Vector3 position, NNConstraint constraint, Node hint = null) {
			
			if (graphNodes == null || depth*width != graphNodes.Length) {
				//Debug.LogError ("NavGraph hasn't been generated yet");
				return new NNInfo ();
			}
			
			position = inverseMatrix.MultiplyPoint3x4 (position);
			
			int x = Mathf.Clamp (Mathf.RoundToInt (position.x-0.5F)  , 0, width-1);
			int z = Mathf.Clamp (Mathf.RoundToInt (position.z-0.5F)  , 0, depth-1);
			
			return new NNInfo(nodes[z*width+x]);
		}
		
		public override NNInfo GetNearestForce (Vector3 position, NNConstraint constraint) {
			
			if (graphNodes == null || depth*width != graphNodes.Length) {
				return new NNInfo ();
			}
			
			Vector3 globalPosition = position;
			
			position = inverseMatrix.MultiplyPoint3x4 (position);
			
			int x = Mathf.Clamp (Mathf.RoundToInt (position.x-0.5F)  , 0, width-1);
			int z = Mathf.Clamp (Mathf.RoundToInt (position.z-0.5F)  , 0, depth-1);
			
			Node node = nodes[x+z*width];
			
			Node minNode = null;
			float minDist = float.PositiveInfinity;
			int overlap = getNearestForceOverlap;
			
			if (constraint.Suitable (node)) {
				minNode = node;
				minDist = ((Vector3)minNode.position-globalPosition).sqrMagnitude;
			}
			
			if (minNode != null) {
				if (overlap == 0) return minNode;
				else overlap--;
			}
			
			
			//int counter = 0;
			
			for (int w = 1; w < getNearestForceLimit;w++) {
				int nx = x;
				int nz = z+w;
				
				int nz2 = nz*width;
				
				for (nx = x-w;nx <= x+w;nx++) {
					if (nx < 0 || nz < 0 || nx >= width || nz >= depth) continue;
					//
					if (constraint.Suitable (nodes[nx+nz2])) {
						float dist = ((Vector3)nodes[nx+nz2].position-globalPosition).sqrMagnitude;
						//Debug.DrawRay (nodes[nx+nz2].position,Vector3.up*dist,Color.cyan);counter++;
						if (dist < minDist) { minDist = dist; minNode = nodes[nx+nz2]; }
					}
				}
				
				nz = z-w;
				nz2 = nz*width;
				
				for (nx = x-w;nx <= x+w;nx++) {
					if (nx < 0 || nz < 0 || nx >= width || nz >= depth) continue;
					
					if (constraint.Suitable (nodes[nx+nz2])) {
						float dist = ((Vector3)nodes[nx+nz2].position-globalPosition).sqrMagnitude;
						//Debug.DrawRay (nodes[nx+nz2].position,Vector3.up*dist,Color.cyan);counter++;
						if (dist < minDist) { minDist = dist; minNode = nodes[nx+nz2]; }
					}
				}
				
				nx = x-w;
				nz = z-w+1;
				
				for (nz = z-w+1;nz <= z+w-1; nz++) {
					if (nx < 0 || nz < 0 || nx >= width || nz >= depth) continue;
					
					if (constraint.Suitable (nodes[nx+nz*width])) {
						float dist = ((Vector3)nodes[nx+nz*width].position-globalPosition).sqrMagnitude;
						//Debug.DrawRay (nodes[nx+nz*width].position,Vector3.up*dist,Color.cyan);counter++;
						if (dist < minDist) { minDist = dist; minNode = nodes[nx+nz*width]; }
					}
				}
				
				nx = x+w;
				
				for (nz = z-w+1;nz <= z+w-1; nz++) {
					if (nx < 0 || nz < 0 || nx >= width || nz >= depth) continue;
					
					if (constraint.Suitable (nodes[nx+nz*width])) {
						float dist = ((Vector3)nodes[nx+nz*width].position-globalPosition).sqrMagnitude;
						//Debug.DrawRay (nodes[nx+nz*width].position,Vector3.up*dist,Color.cyan);counter++;
						if (dist < minDist) { minDist = dist; minNode = nodes[nx+nz*width]; }
					}
				}
				
				if (minNode != null) {
					if (overlap == 0) return minNode;
					else overlap--;
				}
			}
			return null;
		}
		
		/** Sets up #neighbourOffsets with the current settings. #neighbourOffsets, #neighbourCosts, #neighbourXOffsets and #neighbourZOffsets are set up.\n
		 * The cost for a non-diagonal movement between two adjacent nodes is RoundToInt (#nodeSize * Int3::Precision)\n
		 * The cost for a diagonal movement between two adjacent nodes is RoundToInt (#nodeSize * Sqrt (2) * Int3::Precision) */
		public virtual void SetUpOffsetsAndCosts () {
			neighbourOffsets = new int[8] {
				-width, 1 , width , -1,
				-width+1, width+1 , width-1, -width-1
			};
			
			int straightCost = Mathf.RoundToInt (nodeSize*Int3.Precision);
			int diagonalCost = Mathf.RoundToInt (nodeSize*Mathf.Sqrt (2F)*Int3.Precision);
			
			neighbourCosts = new int[8] {
				straightCost,straightCost,straightCost,straightCost,
				diagonalCost,diagonalCost,diagonalCost,diagonalCost
			};
			
			//Not for diagonal nodes, first 4 is for the four cross neighbours the last 4 is for the diagonals
			neighbourXOffsets = new int[8] {
				0, 1, 0, -1,
				1, 1, -1, -1
			};
			
			neighbourZOffsets = new int[8] {
				-1, 0, 1, 0,
				-1, 1, 1, -1
			};
		}
		
		public override void Scan () {
			
			AstarPath.OnPostScan += new OnScanDelegate (OnPostScan);
			
			scanns++;
			
			if (nodeSize <= 0) {
				return;
			}
			
			GenerateMatrix ();
			
			if (width > 1024 || depth > 1024) {
				Debug.LogError ("One of the grid's sides is longer than 1024 nodes");
				return;
			}
			
			//GenerateBounds ();
			
			/*neighbourOffsets = new int[8] {
				-width-1,-width,-width+1,
				-1,1,
				width-1,width,width+1
			}*/
			
			SetUpOffsetsAndCosts ();
			
			//GridNode.RemoveGridGraph (this);
			
			int gridIndex = GridNode.SetGridGraph (this);
			
			//graphNodes = new GridNode[width*depth];
			
			nodes = CreateNodes (width*depth);
			graphNodes = nodes as GridNode[];
			
			if (collision == null) {
				collision = new GraphCollision ();
			}
			collision.Initialize (matrix,nodeSize);
			
			
			//Max slope in cosinus
			//float cosAngle = Mathf.Cos (maxSlope*Mathf.Deg2Rad);
			
			for (int z = 0; z < depth; z ++) {
				for (int x = 0; x < width; x++) {
					
					GridNode node = graphNodes[z*width+x];//new GridNode ();
					
					node.SetIndex (z*width+x);
					
					UpdateNodePositionCollision (node,x,z);
					
					
					/*node.position = matrix.MultiplyPoint3x4 (new Vector3 (x+0.5F,0,z+0.5F));
					
					RaycastHit hit;
					
					bool walkable = true;
					
					node.position = collision.CheckHeight (node.position, out hit, out walkable);
					
					node.penalty = 0;//Mathf.RoundToInt (Random.value*100);
					
					//Check if the node is on a slope steeper than permitted
					if (walkable && useRaycastNormal && collision.heightCheck) {
						
						if (hit.normal != Vector3.zero) {
							//Take the dot product to find out the cosinus of the angle it has (faster than Vector3.Angle)
							float angle = Vector3.Dot (hit.normal.normalized,Vector3.up);
							
							if (angle < cosAngle) {
								walkable = false;
							}
						}
					}
					
					//If the walkable flag has already been set to false, there is no point in checking for it again
					if (walkable) {
						node.walkable = collision.Check (node.position);
					} else {
						node.walkable = walkable;
					}*/
					
					node.SetGridIndex (gridIndex);
				}
			}
			
			
			for (int z = 0; z < depth; z ++) {
				for (int x = 0; x < width; x++) {
				
					GridNode node = graphNodes[z*width+x];
						
					CalculateConnections (graphNodes,x,z,node);
					
				}
			}
			
			
			ErodeWalkableArea ();
			//Assign the nodes to the main storage
			
			//startIndex = AstarPath.active.AssignNodes (graphNodes);
			//endIndex = startIndex+graphNodes.Length;
		}
		
		/** Updates position and walkability for the node. Assumes that collision.Initialize (...) has been called before this function */
		public void UpdateNodePositionCollision (Node node, int x, int z) {
			
			node.position = matrix.MultiplyPoint3x4 (new Vector3 (x+0.5F,0,z+0.5F));
			
			RaycastHit hit;
			
			bool walkable = true;
			
			node.position = collision.CheckHeight (node.position, out hit, out walkable);
			
			node.penalty = 0;//Mathf.RoundToInt (Random.value*100);
			
			if (penaltyPosition) {
				node.penalty = Mathf.RoundToInt ((node.position.y-penaltyPositionOffset)*penaltyPositionFactor);
			}
			
			/*if (textureData && textureSourceData != null && x < textureSource.width && z < textureSource.height) {
				for (int i=0;i<3;i++) {
					//node.penalty = textureSourceData
				}
			}*/
			//Check if the node is on a slope steeper than permitted
			if (walkable && useRaycastNormal && collision.heightCheck) {
				
				if (hit.normal != Vector3.zero) {
					//Take the dot product to find out the cosinus of the angle it has (faster than Vector3.Angle)
					float angle = Vector3.Dot (hit.normal.normalized,Vector3.up);
					
					//Add penalty based on normal
					if (penaltyAngle) {
						node.penalty += Mathf.RoundToInt ((1F-angle)*penaltyAngleFactor);
					}
					
					//Max slope in cosinus
					float cosAngle = Mathf.Cos (maxSlope*Mathf.Deg2Rad);
					
					//Check if the slope is flat enough to stand on
					if (angle < cosAngle) {
						walkable = false;
					}
				}
			}
			
			//If the walkable flag has already been set to false, there is no point in checking for it again
			if (walkable) {
				node.walkable = collision.Check (node.position);
			} else {
				node.walkable = walkable;
			}
			
		}
		
		/** Erodes the walkable area. \see #erodeIterations */
		public virtual void ErodeWalkableArea () {
			for (int it=0;it < erodeIterations;it++) {
				for (int z = 0; z < depth; z ++) {
					for (int x = 0; x < width; x++) {
						GridNode node = graphNodes[z*width+x];
						
						if (!node.walkable) {
							
							int index = node.GetIndex ();
							
							for (int i=0;i<4;i++) {
								if (node.GetConnection (i)) {
									nodes[index+neighbourOffsets[i]].walkable = false;
								}
							}
						} else {
							bool anyFalseConnections = false;
						
							for (int i=0;i<4;i++) {
								if (!node.GetConnection (i)) {
									anyFalseConnections = true;
									break;
								}
							}
							
							if (anyFalseConnections) {
								node.walkable = false;
							}
						}
					}
				}
				
				//Recalculate connections
				for (int z = 0; z < depth; z ++) {
					for (int x = 0; x < width; x++) {
						GridNode node = graphNodes[z*width+x];
						CalculateConnections (graphNodes,x,z,node);
					}
				}
			}
			
			
		}
		
		/** Returns true if a connection between the adjacent nodes \a n1 and \a n2 is valid. Also takes into account if the nodes are walkable */
		public virtual bool IsValidConnection (GridNode n1, GridNode n2) {
			if (!n1.walkable || !n2.walkable) {
				return false;
			}
			
			if (maxClimb != 0 && Mathf.Abs (n1.position[maxClimbAxis] - n2.position[maxClimbAxis]) > maxClimb*Int3.Precision) {
				return false;
			}
			
			return true;
		}
		
		/** To reduce memory allocations this array is reused. Used in the CalculateConnections function */
		[System.NonSerialized]
		protected int[] corners;
		
		/** Calculates the grid connections for a single node. Convenience function, it's faster to use CalculateConnections(GridNode[],int,int,node) but that will only show when calculating for a large number of nodes
		  * \todo Test this function, should work ok, but you never know */
		public static void CalculateConnections (GridNode node) {
			GridGraph gg = AstarData.GetGraph (node) as GridGraph;
			
			if (gg != null) {
				int index = node.GetIndex ();
				int x = index % gg.width;
				int z = index / gg.width;
				gg.CalculateConnections (gg.graphNodes,x,z,node);
			}
		}
		
		/** Calculates the grid connections for a single node */
		public virtual void CalculateConnections (GridNode[] graphNodes, int x, int z, GridNode node) {
			
			//Reset all connections
			node.flags = node.flags & -256;
			
			//All connections are disabled if the node is not walkable
			if (!node.walkable) {
				return;
			}
			
			int index = node.GetIndex ();
			
			if (corners == null) {
				corners = new int[4];
			} else {
				for (int i = 0;i<4;i++) {
					corners[i] = 0;
				}
			}
			
			for (int i=0, j = 3; i<4; j = i, i++) {
				
				int nx = x + neighbourXOffsets[i];
				int nz = z + neighbourZOffsets[i];
				
				if (nx < 0 || nz < 0 || nx >= width || nz >= depth) {
					continue;
				}
				
				GridNode other = graphNodes[index+neighbourOffsets[i]];
				
				if (IsValidConnection (node, other)) {
					node.SetConnectionRaw (i,1);
					
					corners[i]++;
					corners[j]++;
				}
			}
			
			if (neighbours == NumNeighbours.Eight) {
				if (cutCorners) {
					for (int i=0; i<4; i++) {
						
						if (corners[i] >= 1) {
							int nx = x + neighbourXOffsets[i+4];
							int nz = z + neighbourZOffsets[i+4];
						
							if (nx < 0 || nz < 0 || nx >= width || nz >= depth) {
								continue;
							}
					
							GridNode other = graphNodes[index+neighbourOffsets[i+4]];
							
							if (IsValidConnection (node,other)) {
								node.SetConnectionRaw (i+4,1);
							}
						}
					}
				} else {
					for (int i=0; i<4; i++) {
						
						//We don't need to check if it is out of bounds because if both of the other neighbours are inside the bounds this one must be too
						if (corners[i] == 2) {
							GridNode other = graphNodes[index+neighbourOffsets[i+4]];
							
							if (IsValidConnection (node,other)) {
								node.SetConnectionRaw (i+4,1);
							}
						}
					}
				}
			}
			
		}
		
		/** Auto links grid graphs together. Called after all graphs have been scanned.
		 * \see autoLinkGrids
		 */
		public void OnPostScan (AstarPath script) {
			
			AstarPath.OnPostScan -= new OnScanDelegate (OnPostScan);
			
			if (!autoLinkGrids || autoLinkDistLimit <= 0) {
				return;
			}
			
			//Link to other grids
			
			int maxCost = Mathf.RoundToInt (autoLinkDistLimit * Int3.Precision);
			
			//Loop through all GridGraphs
			foreach (GridGraph gg in script.astarData.FindGraphsOfType (typeof (GridGraph))) {
				
				if (gg == this || gg.nodes == null || nodes == null) {
					continue;
				}
				
				//Int3 prevPos = gg.GetNearest (nodes[0]).position;
				
				//Z = 0
				for (int x = 0; x < width;x++) {
					
					Node node1 = nodes[x];
					Node node2 = gg.GetNearest (node1.position);
					
					Vector3 pos = inverseMatrix.MultiplyPoint3x4 (node2.position);
					
					if (pos.z > 0) {
						continue;
					}
					
					int cost = (int)(node1.position-node2.position).magnitude;
					
					if (cost > maxCost) {
						continue;
					}
					
					node1.AddConnection (node2,cost);
					node2.AddConnection (node1,cost);
				}
				
				//X = 0
				for (int z = 0; z < depth;z++) {
					
					Node node1 = nodes[z*width];
					Node node2 = gg.GetNearest (node1.position);
					
					Vector3 pos = inverseMatrix.MultiplyPoint3x4 (node2.position);
					
					if (pos.x > 0) {
						continue;
					}
					
					int cost = (int)(node1.position-node2.position).magnitude;
					
					if (cost > maxCost) {
						continue;
					}
					
					node1.AddConnection (node2,cost);
					node2.AddConnection (node1,cost);
				}
				
				//Z = max
				for (int x = 0; x < width;x++) {
					
					Node node1 = nodes[(depth-1)*width+x];
					Node node2 = gg.GetNearest (node1.position);
					
					Vector3 pos = inverseMatrix.MultiplyPoint3x4 (node2.position);
					
					if (pos.z < depth-1) {
						continue;
					}
					
					//Debug.DrawLine (node1.position,node2.position,Color.red);
					int cost = (int)(node1.position-node2.position).magnitude;
					
					if (cost > maxCost) {
						continue;
					}
					
					node1.AddConnection (node2,cost);
					node2.AddConnection (node1,cost);
				}
				
				//X = max
				for (int z = 0; z < depth;z++) {
					
					Node node1 = nodes[z*width+width-1];
					Node node2 = gg.GetNearest (node1.position);
					
					Vector3 pos = inverseMatrix.MultiplyPoint3x4 (node2.position);
					
					if (pos.x < width-1) {
						continue;
					}
					
					int cost = (int)(node1.position-node2.position).magnitude;
					
					if (cost > maxCost) {
						continue;
					}
					
					
					
					node1.AddConnection (node2,cost);
					node2.AddConnection (node1,cost);
				}
			}
	
		}
		
		public override void OnDrawGizmos (bool drawNodes) {
			
			//GenerateMatrix ();
			
			Gizmos.matrix = boundsMatrix;
			Gizmos.color = Color.white;
			Gizmos.DrawWireCube (Vector3.zero, new Vector3 (size.x,0,size.y));
			
			Gizmos.matrix = Matrix4x4.identity;
			
			if (!drawNodes) {
				return;
			}
			
			if (graphNodes == null || depth*width != graphNodes.Length) {
				//Scan (AstarPath.active.GetGraphIndex (this));
				return;
			}
			
			base.OnDrawGizmos (drawNodes);
			for (int z = 0; z < depth; z ++) {
				for (int x = 0; x < width; x++) {
					GridNode node = graphNodes[z*width+x];
					
					if (!node.walkable) {// || node.activePath != AstarPath.active.debugPath)  
						continue;
					}
					//Gizmos.color = node.walkable ? Color.green : Color.red;
					//Gizmos.DrawSphere (node.position,0.2F);
					
					Gizmos.color = NodeColor (node);
					
					//if (true) {
					//	Gizmos.DrawCube (node.position,Vector3.one*nodeSize);
					//}
					//else 
					if (AstarPath.active.showSearchTree && Node.activePath != null && AstarPath.active.debugPath != null) {
						if (node.pathID == AstarPath.active.debugPath.pathID && node.parent != null) {
							Gizmos.DrawLine (node.position, node.parent.position);
						}
					} else {
						
						for (int i=0;i<8;i++) {
							
							if (node.GetConnection (i)) {
								GridNode other = graphNodes[node.GetIndex ()+neighbourOffsets[i]];
								Gizmos.DrawLine (node.position, other.position);
							}
						}
					}
					
				}
			}
		}
		
		public void UpdateArea (GraphUpdateObject o) {
			
			if (graphNodes == null || nodes == null) {
				Debug.LogWarning ("The Grid Graph is not scanned, cannot update area ");
				//Not scanned
				return;
			}
			
			//Copy the bounds
			Bounds b = o.bounds;
			
			//Matrix inverse 
			//node.position = matrix.MultiplyPoint3x4 (new Vector3 (x+0.5F,0,z+0.5F));
			
			Vector3 min = inverseMatrix.MultiplyPoint3x4 (b.min);
			Vector3 max = inverseMatrix.MultiplyPoint3x4 (b.max);
			
			int minX = Mathf.RoundToInt (min.x-0.5F);
			int maxX = Mathf.RoundToInt (max.x-0.5F);
			
			int minZ = Mathf.RoundToInt (min.z-0.5F);
			int maxZ = Mathf.RoundToInt (max.z-0.5F);
			
			//We now have coordinates in local space (i.e 1 unit = 1 node)
			
			int ominx = minX;
			int omaxx = maxX;
			int ominz = minZ;
			int omaxz = maxZ;
			
			if (o.updatePhysics && !o.modifyWalkability) {
				//Add the collision.diameter margin for physics calls
				if (collision.collisionCheck) {
					Vector3 margin = new Vector3 (collision.diameter,0,collision.diameter)*0.5F;
				
					min -= margin*1.02F;//0.02 safety margin, physics is rarely very accurate
					max += margin*1.02F;
					Debug.DrawLine (matrix.MultiplyPoint3x4(min),matrix.MultiplyPoint3x4(max),Color.cyan);
					
					minX = Mathf.RoundToInt (min.x-0.5F);
					maxX = Mathf.RoundToInt (max.x-0.5F);
				
					minZ = Mathf.RoundToInt (min.z-0.5F);
					maxZ = Mathf.RoundToInt (max.z-0.5F);
					
				}
				
				collision.Initialize (matrix,nodeSize);
				
				for (int x = minX;x <= maxX;x++) {
					for (int z = minZ;z <= maxZ;z++) {
						
						if (x < 0 || z < 0) {
							continue;
						}
						if (z >= depth || x >= width) {
							break;
						}
						
						int index = z*width+x;
						
						GridNode node = graphNodes[index];
						
						//Register that this node will eventually have some settings changed
						o.WillUpdateNode (node);
						
						UpdateNodePositionCollision (node,x,z);
					}
				}
			}
			
			//This is the area inside the bounding box, call Apply on it
			for (int x = ominx;x <= omaxx;x++) {
				for (int z = ominz;z <= omaxz;z++) {
					
					if (x < 0 || z < 0) {
						continue;
					}
					if (z >= depth || x >= width) {
						break;
					}
					
					int index = z*width+x;
					
					GridNode node = graphNodes[index];
					
					if (!o.updatePhysics || o.modifyWalkability) {
						//Register that this node will eventually have some settings changed
						//If the above IF evaluates to false, the node will already have been added before in the function
						o.WillUpdateNode (node);
					}
					
					o.Apply (node);
				}
			}
			
			//Recalculate connections
			if (o.updatePhysics || o.modifyWalkability) {
				//Add some margin
				minX--; //Mathf.Clamp (minX-1,0,width);
				maxX++; //Mathf.Clamp (maxX+1,0,width);
				minZ--; //Mathf.Clamp (minZ-1,0,depth);
				maxZ++; //Mathf.Clamp (maxZ+1,0,depth);
				
				for (int x = minX;x <= maxX;x++) {
					for (int z = minZ;z <= maxZ;z++) {
						
						if (x < 0 || z < 0 || x >= width || z >= depth) {
							continue;
						}
						
						int index = z*width+x;
						
						GridNode node = graphNodes[index];
						
						CalculateConnections (graphNodes,x,z,node);
						
					}
				}
			}
		}
		
		/** Serializes grid graph specific node stuff to the serializer.
		 * \astarpro */
		public void SerializeNodes (Node[] nodes, AstarSerializer serializer) {

			/** \todo Add check for mask == Save Node Positions. Can't use normal mask since it is changed by SerializeSettings */
			
			GenerateMatrix ();
			
			int maxValue = 0;
			int minValue = 0;
			
			for (int i=0;i<nodes.Length;i++) {
				int val = nodes[i].position.y;
				maxValue = val > maxValue ? val : maxValue;
				minValue = val < minValue ? val : minValue;
			}
			
			int maxRange = maxValue > -minValue ? maxValue : -minValue;
			
			if (maxRange <= System.Int16.MaxValue) {
				//Int16
				serializer.writerStream.Write ((byte)0);
				for (int i=0;i<nodes.Length;i++) {
					serializer.writerStream.Write ((System.Int16)(inverseMatrix.MultiplyPoint3x4 ((Vector3)nodes[i].position).y));
				}
			} else {
				//Int32
				serializer.writerStream.Write ((byte)1);
				for (int i=0;i<nodes.Length;i++) {
					serializer.writerStream.Write (inverseMatrix.MultiplyPoint3x4 ((Vector3)nodes[i].position).y);
				}
			}
			/*for (int i=0;i<nodes.Length;i++) {
				GridNode node = nodes[i] as GridNode;
				
				if (node == null) {
					Debug.LogError ("Serialization Error : Couldn't cast the node to the appropriate type - GridGenerator");
					return;
				}
				
				serializer.writerStream.Write (node.index);
			}*/

		}
		
		/** Deserializes grid graph specific node stuff from the serializer.
		 * \astarpro */
		public void DeSerializeNodes (Node[] nodes, AstarSerializer serializer) {
			

			/*for (int i=0;i<nodes.Length;i++) {
				GridNode node = nodes[i] as GridNode;
				
				if (node == null) {
					Debug.LogError ("DeSerialization Error : Couldn't cast the node to the appropriate type - GridGenerator");
					return;
				}*/
			
			GenerateMatrix ();
			
			SetUpOffsetsAndCosts ();
			
			if (nodes == null || nodes.Length == 0) {
				return;
			}
			
			graphNodes = new GridNode[nodes.Length];
			
			int gridIndex = GridNode.SetGridGraph (this);
			
			int numberSize = (int)serializer.readerStream.ReadByte ();
			
			for (int z = 0; z < depth; z ++) {
				for (int x = 0; x < width; x++) {
					
					GridNode node = nodes[z*width+x] as GridNode;
				
					graphNodes[z*width+x] = node;
					
					if (node == null) {
						Debug.LogError ("DeSerialization Error : Couldn't cast the node to the appropriate type - GridGenerator");
						return;
					}
					
					node.SetIndex  (z*width+x);
					node.SetGridIndex (gridIndex);
					
					
					float yPos = 0;
					
					//if (serializer.mask == AstarSerializer.SMask.SaveNodePositions) {
						//Needs to multiply with precision factor because the position will be scaled by Int3.Precision later (Vector3 --> Int3 conversion)
					if (numberSize == 0) {
						yPos = serializer.readerStream.ReadInt16 ();
					} else {
						yPos = serializer.readerStream.ReadInt32 ();
					}
					//}
					
					node.position = matrix.MultiplyPoint3x4 (new Vector3 (x+0.5F,yPos,z+0.5F));
				}
			}

			
		}
		
		//IFunnelGraph Implementation
		
		public void BuildFunnelCorridor (Node[] path, int sIndex, int eIndex, List<Vector3> left, List<Vector3> right) {
			
			for (int n=sIndex;n<eIndex;n++) {
				
				GridNode n1 = path[n] as GridNode;
				GridNode n2 = path[n+1] as GridNode;
				
				AddPortal (n1,n2,left,right);
			}
		}
		
		public void AddPortal (Node n1, Node n2, List<Vector3> left, List<Vector3> right) {
			//Not implemented
		}
		
		public void AddPortal (GridNode n1, GridNode n2, List<Vector3> left, List<Vector3> right) {
			
			if (n1 == n2) {
				return;
			}
			
			int i1 = n1.GetIndex ();
			int i2 = n2.GetIndex ();
			int x1 = i1 % width;
			int x2 = i2 % width;
			int z1 = i1 / width;
			int z2 = i2 / width;
			
			Vector3 n1p = n1.position;
			Vector3 n2p = n2.position;
			
			int diffx = Mathf.Abs (x1-x2);
			int diffz = Mathf.Abs (z1-z2);
			
			if (diffx > 1 || diffz > 1) {
				//If the nodes are not adjacent to each other
				
				left.Add (n1p);
				right.Add (n1p);
				left.Add (n2p);
				right.Add (n2p);
			} else if ((diffx+diffz) <= 1){
				//If it is not a diagonal move
				
				Vector3 dir = n2p - n1p;
				dir = dir.normalized * nodeSize * 0.5F;
				Vector3 tangent = Vector3.Cross (dir, Vector3.up);
				tangent = tangent.normalized * nodeSize * 0.5F;
				
				left.Add (n1p + dir - tangent);
				right.Add (n1p + dir + tangent);
			} else {
				//Diagonal move
				
				Node t1 = nodes[z1 * width + x2];
				Node t2 = nodes[z2 * width + x1];
				Node target = null;
				
				if (t1.walkable) {
					target = t1;
				} else if (t2.walkable) {
					target = t2;
				}
				
				if (target == null) {
					Vector3 avg = (n1p + n2p) * 0.5F;
					
					left.Add (avg);
					right.Add (avg);
				} else {
					AddPortal (n1,(GridNode)target,left,right);
					AddPortal ((GridNode)target,n2,left,right);
				}
			}
		}
		
		//END IFunnelGraph Implementation
		
		
		public bool CheckConnection (GridNode node, int dir) {
			if (neighbours == NumNeighbours.Eight) {
				return node.GetConnection (dir);
			} else {
				int dir1 = (dir-4-1) & 0x3;
				int dir2 = (dir-4+1) & 0x3;
				
				if (!node.GetConnection (dir1) || !node.GetConnection (dir2)) {
					return false;
				} else {
					GridNode n1 = nodes[node.GetIndex ()+neighbourOffsets[dir1]] as GridNode;
					GridNode n2 = nodes[node.GetIndex ()+neighbourOffsets[dir2]] as GridNode;
					
					if (!n1.walkable || !n2.walkable) {
						return false;
					}
					
					if (!n2.GetConnection (dir1) || !n1.GetConnection (dir2)) {
						return false;
					}
				}
				return true;
			}
		}
		
		/*
		function line(x0, y0, x1, y1)
   dx := abs(x1-x0)
   dy := abs(y1-y0) 
   if x0 < x1 then sx := 1 else sx := -1
   if y0 < y1 then sy := 1 else sy := -1
   err := dx-dy
 
   loop
     setPixel(x0,y0)
     if x0 = x1 and y0 = y1 exit loop
     e2 := 2*err
     if e2 > -dy then 
       err := err - dy
       x0 := x0 + sx
     end if
     if e2 <  dx then 
       err := err + dx
       y0 := y0 + sy 
     end if
   end loop
   */
		public void SerializeSettings (AstarSerializer serializer) {
			serializer.mask -= AstarSerializer.SMask.SaveNodePositions;
			
			//serializer.AddValue ("Width",width);
			//serializer.AddValue ("Depth",depth);
			//serializer.AddValue ("Height",height);
			
			serializer.AddValue ("unclampedSize",unclampedSize);
			
			serializer.AddValue ("cutCorners",cutCorners);
			serializer.AddValue ("neighbours",(int)neighbours);
			
			serializer.AddValue ("center",center);
			serializer.AddValue ("rotation",rotation);
			serializer.AddValue ("nodeSize",nodeSize);
			serializer.AddValue ("collision",collision == null ? new GraphCollision () : collision);
			
			serializer.AddValue ("maxClimb",maxClimb);
			serializer.AddValue ("maxClimbAxis",maxClimbAxis);
			serializer.AddValue ("maxSlope",maxSlope);
			
			serializer.AddValue ("erodeIterations",erodeIterations);
			
			serializer.AddValue ("penaltyAngle",penaltyAngle);
			serializer.AddValue ("penaltyAngleFactor",penaltyAngleFactor);
			serializer.AddValue ("penaltyPosition",penaltyPosition);
			serializer.AddValue ("penaltyPositionOffset",penaltyPositionOffset);
			serializer.AddValue ("penaltyPositionFactor",penaltyPositionFactor);
			
			serializer.AddValue ("aspectRatio",aspectRatio);
			
		}
		
		public void DeSerializeSettings (AstarSerializer serializer) {
			
			//width = (int)serializer.GetValue ("Width",typeof(int));
			//depth = (int)serializer.GetValue ("Depth",typeof(int));
			//height = (float)serializer.GetValue ("Height",typeof(float));
			
			unclampedSize = (Vector2)serializer.GetValue ("unclampedSize",typeof(Vector2));
			
			cutCorners = (bool)serializer.GetValue ("cutCorners",typeof(bool));
			neighbours = (NumNeighbours)serializer.GetValue ("neighbours",typeof(int));
			
			rotation = (Vector3)serializer.GetValue ("rotation",typeof(Vector3));
			
			nodeSize = (float)serializer.GetValue ("nodeSize",typeof(float));
			
			collision = (GraphCollision)serializer.GetValue ("collision",typeof(GraphCollision));
			
			center = (Vector3)serializer.GetValue ("center",typeof(Vector3));
			
			maxClimb = (float)serializer.GetValue ("maxClimb",typeof(float));
			maxClimbAxis = (int)serializer.GetValue ("maxClimbAxis",typeof(int),1);
			maxSlope = (float)serializer.GetValue ("maxSlope",typeof(float),90.0F);
			
			erodeIterations = (int)serializer.GetValue ("erodeIterations",typeof(int));
			
			penaltyAngle = 			(bool)serializer.GetValue ("penaltyAngle",typeof(bool));
			penaltyAngleFactor = 	(float)serializer.GetValue ("penaltyAngleFactor",typeof(float));
			penaltyPosition = 		(bool)serializer.GetValue ("penaltyPosition",typeof(bool));
			penaltyPositionOffset = (float)serializer.GetValue ("penaltyPositionOffset",typeof(float));
			penaltyPositionFactor = (float)serializer.GetValue ("penaltyPositionFactor",typeof(float));
			
			aspectRatio = (float)serializer.GetValue ("aspectRatio",typeof(float),1F);
			
			
			Matrix4x4 oldMatrix = matrix;
				
			GenerateMatrix ();
			SetUpOffsetsAndCosts ();
			
			if (serializer.onlySaveSettings) {
				if (oldMatrix != matrix && nodes != null) {
					AstarPath.active.AutoScan ();
				}
			}
			
			//Debug.Log ((string)serializer.GetValue ("SomeString",typeof(string)));
			//Debug.Log ((Bounds)serializer.GetValue ("SomeBounds",typeof(Bounds)));
		}
	}
	
	public class GridNode : Node {
		
		//Flags used
		//last 8 bits - see Node class
		//First 8 bits for connectivity info inside this grid
		
		//Size = [Node, 28 bytes] + 4 = 32 bytes
		
		//First 24 bits used for the index value of this node in the graph specified by the last 8 bits
		protected int indices;
		
		public static GridGraph[] gridGraphs;
		
		public bool HasAnyGridConnections () {
			return (flags & 0xFF) != 0;
		}
		
		public bool GetConnection (int i) {
			return ((flags >> i) & 1) == 1;
		}
		
		public void SetConnection (int i, int value) {
			flags = flags & ~(1 << i) | (value << i);
		}
		
		//Sets a connection without clearing the previous value, faster if you are setting all connections at once and have cleared the value before calling this function
		public void SetConnectionRaw (int i, int value) {
			flags = flags | (value << i);
		}
		
		public int GetGridIndex () {
			return indices >> 24;
		}
		
		public int GetIndex () {
			return indices & 0xFFFFFF;
		}
		
		public void SetIndex (int i) {
			indices &= ~0xFFFFFF;
			indices |= i;
		}
		
		public void SetGridIndex (int gridIndex) {
			indices &= 0xFFFFFF;
			indices |= gridIndex << 24;
		}
		
		/*public override bool ContainsConnection (Node node, Path p) {
			if (!node.IsWalkable (p)) {
				return false;
			}
			
			if (connections != null) {
				for (int i=0;i<connections.Length;i++) {
					if (connections[i] == node) {
						return true;
					}
				}
			}
			
			int index = indices & 0xFFFFFF;
			
			int[] neighbourOffsets = gridGraphs[indices >> 24].neighbourOffsets;
			GridNode[] nodes = gridGraphs[indices >> 24].graphNodes;
			
			for (int i=0;i<8;i++) {
				if (((flags >> i) & 1) == 1) {
					
					Node other = nodes[index+neighbourOffsets[i]];
					if (other == node) {
						return true;
					}
				}
			}
			return false;
		}*/
		
		/** Updates the grid connections of this node and it's neighbour nodes to reflect walkability of this node */
		public void UpdateGridConnections () {
			
			GridGraph graph = gridGraphs[indices >> 24];
			
			int index = indices & 0xFFFFFF;
			
			int x = index % graph.width;
			int z = index/graph.width;
			graph.CalculateConnections (graph.graphNodes, x, z, this);
			
			int[] neighbourOffsets = graph.neighbourOffsets;
			int[] neighbourOffsetsX = graph.neighbourXOffsets;
			int[] neighbourOffsetsZ = graph.neighbourZOffsets;
			
			for (int i=0;i<8;i++) {
				//if (((flags >> i) & 1) == 1) {
				
				int nx = x+neighbourOffsetsX[i];
				int nz = z+neighbourOffsetsZ[i];
				
				
				if (nx < 0 || nz < 0 || nx >= graph.width || nz >= graph.depth) {
					continue;
				}
				
				GridNode node = (GridNode)graph.nodes[index+neighbourOffsets[i]];
				
				graph.CalculateConnections (graph.graphNodes, nx, nz, node);
				//}
			}
			
		}
		
		/** Removes a connection from the node.
		 * This can be a standard connection or a grid connection
		 * \returns True if a connection was removed, false otherwsie */
		public override bool RemoveConnection (Node node) {
			
			bool standard = base.RemoveConnection (node);
			
			GridGraph graph = gridGraphs[indices >> 24];
			
			int index = indices & 0xFFFFFF;
			
			int x = index % graph.width;
			int z = index/graph.width;
			graph.CalculateConnections (graph.graphNodes, x, z, this);
			
			int[] neighbourOffsets = graph.neighbourOffsets;
			int[] neighbourOffsetsX = graph.neighbourXOffsets;
			int[] neighbourOffsetsZ = graph.neighbourZOffsets;
			
			for (int i=0;i<8;i++) {
				
				int nx = x+neighbourOffsetsX[i];
				int nz = z+neighbourOffsetsZ[i];
				
				if (nx < 0 || nz < 0 || nx >= graph.width || nz >= graph.depth) {
					continue;
				}
				
				GridNode gNode = (GridNode)graph.nodes[index+neighbourOffsets[i]];
				if (gNode == node) {
					SetConnection (i,0);
					return true;
				}
			}
			return standard;
		}
		
		public override void UpdateConnections () {
			base.UpdateConnections ();
			UpdateGridConnections ();
		}
		
		public 
	override
		void UpdateAllG (BinaryHeap open) {
			//g = parent.g+cost+penalty;
			//f = g+h;
			
			base.UpdateAllG (open);	
			
			//Called in the base function
			//open.Add (this);
			
			int index = indices & 0xFFFFFF;
			
			int[] neighbourOffsets = gridGraphs[indices >> 24].neighbourOffsets;
			GridNode[] nodes = gridGraphs[indices >> 24].graphNodes;
			
			for (int i=0;i<8;i++) {
				if (((flags >> i) & 1) == 1) {
					
					Node node = nodes[index+neighbourOffsets[i]];
					
					if (node.parent == this && node.pathID == pathID) {
						node.UpdateAllG (open);
					}
				}
			}
				
		}
		
		public override void FloodFill (Stack<Node> stack, int area) {
			
			base.FloodFill (stack,area);
			
			GridGraph graph = gridGraphs[indices >> 24];//];
			
			int index = indices & 0xFFFFFF;
			
			int[] neighbourOffsets = graph.neighbourOffsets;
			//int[] neighbourCosts = graph.neighbourCosts;
			GridNode[] nodes = graph.graphNodes;
			
			for (int i=0;i<8;i++) {
				if (((flags >> i) & 1) == 1) {
					
					Node node = nodes[index+neighbourOffsets[i]];
					
					if (node.walkable && node.area != area) {
						stack.Push (node);
						node.area = area;
					}
				}
			}
		}
		
		public override int[] InitialOpen (BinaryHeap open, Int3 targetPosition, Int3 position, Path path, bool doOpen) {
			
			if (doOpen) {
				Open (open,targetPosition,path);
			}
			
			return base.InitialOpen (open,targetPosition,position,path,doOpen);
			
		}
		
		public 
	override
	void Open (BinaryHeap open, Int3 targetPosition, Path path) {
			
			base.Open (open, targetPosition, path);
			
			GridGraph graph = gridGraphs[indices >> 24];
			
			int[] neighbourOffsets = graph.neighbourOffsets;
			int[] neighbourCosts = graph.neighbourCosts;
			GridNode[] nodes = graph.graphNodes;
			
			int index = indices & 0xFFFFFF;
			
			for (int i=0;i<8;i++) {
				if (((flags >> i) & 1) == 1) {
					
					Node node = nodes[index+neighbourOffsets[i]];
					
					if (!path.CanTraverse (node)) continue;
					
					if (node.pathID != pathID) {
						
						node.parent = this;
						node.pathID = pathID;
						
						node.cost = neighbourCosts[i];
						
						node.UpdateH (targetPosition,path.heuristic,path.heuristicScale);
						node.UpdateG ();
						
						
						open.Add (node);
					
					} else {
						//If not we can test if the path from the current node to this one is a better one then the one already used
						int tmpCost = neighbourCosts[i];//(current.costs == null || current.costs.Length == 0 ? costs[current.neighboursKeys[i]] : current.costs[current.neighboursKeys[i]]);
						
						if (g+tmpCost+node.penalty < node.g) {
							node.cost = tmpCost;
							//node.extraCost = extraCost2;
							node.parent = this;;
							
							node.UpdateAllG (open);
							
							//open.Add (node);
							//Debug.DrawLine (current.vectorPos,current.neighbours[i].vectorPos,Color.cyan); //Uncomment for @Debug
						}
						
						 else if (node.g+tmpCost+penalty < g) {//Or if the path from this node ("node") to the current ("current") is better
							/*bool contains = false;
							
							//[Edit, no one-way links between nodes in a single grid] Make sure we don't travel along the wrong direction of a one way link now, make sure the Current node can be accesed from the Node.
							/*for (int y=0;y<node.connections.Length;y++) {
								if (node.connections[y].endNode == this) {
									contains = true;
									break;
								}
							}
							
							if (!contains) {
								continue;
							}*/
							
							parent = node;
							cost = tmpCost;
							//extraCost = extraCost2;
							
							UpdateAllG (open);
							//open.Add (this);
							//Debug.DrawLine (current.vectorPos,current.neighbours[i].vectorPos,Color.blue); //Uncomment for @Debug
							
							//open.Add (this);
						}
					}
				}
			}
		}
		
		public static void RemoveGridGraph (GridGraph graph) {
			
			if (gridGraphs == null) {
				return;
			}
			
			for (int i=0;i<gridGraphs.Length;i++) {
				if (gridGraphs[i] == graph) {
					
					if (gridGraphs.Length == 1) {
						gridGraphs = null;
						return;
					}
					
					for (int j=i+1;j<gridGraphs.Length;j++) {
						GridGraph gg = gridGraphs[j];
						
						if (gg.nodes != null) {
							for (int n=0;n<gg.nodes.Length;n++) {
								((GridNode)gg.nodes[n]).SetGridIndex (j-1);
							}
						}
					}
					
					GridGraph[] tmp = new GridGraph[gridGraphs.Length-1];
					for (int j=0;j<i;j++) {
						tmp[j] = gridGraphs[j];
					}
					for (int j=i+1;j<gridGraphs.Length;j++) {
						tmp[j-1] = gridGraphs[j];
					}
					return;
				}
			}	
		}
		
		public static int SetGridGraph (GridGraph graph) {
			if (gridGraphs == null) {
				gridGraphs = new GridGraph[1];
			} else {
				
				for (int i=0;i<gridGraphs.Length;i++) {
					if (gridGraphs[i] == graph) {
						return i;
					}
				}
				
				GridGraph[] tmp = new GridGraph[gridGraphs.Length+1];
				for (int i=0;i<gridGraphs.Length;i++) {
					tmp[i] = gridGraphs[i];
				}
				gridGraphs = tmp;
			}
			
			gridGraphs[gridGraphs.Length-1] = graph;
			return gridGraphs.Length-1;
		}
	}
	
	public enum NumNeighbours {
		Four,
		Eight
	}
}