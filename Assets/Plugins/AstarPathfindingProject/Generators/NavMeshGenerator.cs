//#define SafeIntMath  //Deprectated
//#define DEBUGGING    //Some debugging
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pathfinding {
	public interface INavmesh {
		Int3[] vertices {
			get;
			set;
		}
		
		/** Bounding Box Tree */
		BBTree bbTree {
			get;
			set;
		}
		
		//Int3[] originalVertices {
		//	get;
		//	set;
		//}
	}
	
	[System.Serializable]
	/** Generates graphs based on navmeshes.
	 * \ingroup graphs
	 * Navmeshes are meshes where each polygon define a walkable area.
	 */
	public class NavMeshGraph : NavGraph, INavmesh, ISerializableGraph, IUpdatableGraph, IFunnelGraph
	{
		
		public override Node[] CreateNodes (int number) {
			MeshNode[] tmp = new MeshNode[number];
			for (int i=0;i<number;i++) {
				tmp[i] = new MeshNode ();
			}
			return tmp as Node[];
		}
		
		
		public Mesh sourceMesh; /**< Mesh to construct navmesh from */
		
		public Vector3 offset; /**< Offset in world space */
		public Vector3 rotation; /**< Rotation in degrees */
		public float scale = 1; /**< Scale of the graph */
		
		//public Node[] graphNodes;
		
		/** Bounding Box Tree. Enables really fast lookups of nodes. \astarpro */
		BBTree _bbTree;
		public BBTree bbTree {
			get { return _bbTree; }
			set { _bbTree = value;}
		}
		
		[System.NonSerialized]
		Int3[] _vertices;
		
		public Int3[] vertices {
			get {
				return _vertices;
			}
			set {
				_vertices = value;
			}
		}
		
		[System.NonSerialized]
		Vector3[] originalVertices;
		
		//[System.NonSerialized]
		//Int3[] _originalVertices;
		Matrix4x4 _originalMatrix;
		
		/*public Int3[] originalVertices {
			get { 	return _originalVertices; 	}
			set { 	_originalVertices = value;	}
		}*/
		
		[System.NonSerialized]
		public int[] triangles;
		
		public void GenerateMatrix () {
			
			matrix = Matrix4x4.TRS (offset,Quaternion.Euler (rotation),new Vector3 (scale,scale,scale));
			
		}
		
		//Relocates the nodes to match the newMatrix, the "oldMatrix" variable can be left out in this function call (only for this graph generator) since it is not used
		public override void RelocateNodes (Matrix4x4 oldMatrix, Matrix4x4 newMatrix) {
			//base.RelocateNodes (oldMatrix,newMatrix);
			
			if (vertices == null || vertices.Length == 0 || originalVertices == null || originalVertices.Length != vertices.Length) {
				return;
			}
			
			for (int i=0;i<vertices.Length;i++) {
				//Vector3 tmp = inv.MultiplyPoint3x4 (vertices[i]);
				//vertices[i] = (Int3)newMatrix.MultiplyPoint3x4 (tmp);
				vertices[i] = newMatrix.MultiplyPoint3x4 (originalVertices[i]);
			}
			
			for (int i=0;i<nodes.Length;i++) {
				MeshNode node = (MeshNode)nodes[i];
				node.position = (vertices[node.v1]+vertices[node.v2]+vertices[node.v3])/3F;
				
				if (node.connections != null) {
					for (int q=0;q<node.connections.Length;q++) {
						node.connectionCosts[q] = (node.position-node.connections[q].position).costMagnitude;
					}
				}
			}
		}
	
		public static NNInfo GetNearest (INavmesh graph, Node[] nodes, Vector3 position, NNConstraint constraint) {
				
			if (nodes == null || nodes.Length == 0) {
				Debug.LogError ("NavGraph hasn't been generated yet or does not contain any nodes");
				return new NNInfo ();
			}
			
			if (constraint == null) constraint = NNConstraint.None;
			
			
			return GetNearestForce (nodes,graph.vertices, position, constraint);
			
		}
		
		public override NNInfo GetNearest (Vector3 position, NNConstraint constraint, Node hint = null) {
			return GetNearest (this, nodes,position, constraint);
		}
		
		/** This performs a linear search through all polygons returning the closest one.
		 * This is usually only called in the Free version of the A* Pathfinding Project since the Pro one supports BBTrees and will do another query
		 */
		public override NNInfo GetNearestForce (Vector3 position, NNConstraint constraint) {
			
			return GetNearestForce (nodes,vertices,position,constraint);
			//Debug.LogWarning ("This function shouldn't be called since constrained nodes are sent back in the GetNearest call");
			
			//return new NNInfo ();
		}
		
		/** This performs a linear search through all polygons returning the closest one */
		public static NNInfo GetNearestForce (Node[] nodes, Int3[] vertices, Vector3 position, NNConstraint constraint) {
			Int3 pos = (Int3)position;
			//Replacement for Infinity, the maximum value a int can hold
			int minDist = -1;
			Node minNode = null;
			
			float minDist2 = -1;
			Node minNode2 = null;
			
			int minConstDist = -1;
			Node minNodeConst = null;
			
			float minConstDist2 = -1;
			Node minNodeConst2 = null;
			
			//int rnd = (int)Random.Range (0,10000);
			
			//int skipped = 0;
			
			
			for (int i=0;i<nodes.Length;i++) {
				MeshNode node = nodes[i] as MeshNode;
				
				if (!Polygon.IsClockwise (vertices[node.v1],vertices[node.v2],pos) || !Polygon.IsClockwise (vertices[node.v2],vertices[node.v3],pos) || !Polygon.IsClockwise (vertices[node.v3],vertices[node.v1],pos))
				{
				//Polygon.TriangleArea2 (vertices[node.v1],vertices[node.v2],pos) >= 0 || Polygon.TriangleArea2 (vertices[node.v2],vertices[node.v3],pos) >= 0 || Polygon.TriangleArea2 (vertices[node.v3],vertices[node.v1],pos) >= 0) {
					
					/*if (minDist2 != -1) {
						float d1 = (node.position-vertices[node.v1]).sqrMagnitude;
						d1 = Mathf.Min (d1,(node.position-vertices[node.v1]).sqrMagnitude);
						d1 = Mathf.Min (d1,(node.position-vertices[node.v1]).sqrMagnitude);
						
						//The closest distance possible from the current node to 'pos'
						d1 = (node.position-pos).sqrMagnitude-d1;
						
						if (d1 > minDist2) {
							skipped++;
							continue;
						}
					}*/
					
					/*float dist2 = Mathfx.DistancePointSegment2 (pos.x,pos.z,vertices[node.v1].x,vertices[node.v1].z,vertices[node.v2].x,vertices[node.v2].z);
					dist2 = Mathfx.Min (dist2,Mathfx.DistancePointSegment2 (pos.x,pos.z,vertices[node.v1].x,vertices[node.v1].z,vertices[node.v3].x,vertices[node.v3].z));
					dist2 = Mathfx.Min (dist2,Mathfx.DistancePointSegment2 (pos.x,pos.z,vertices[node.v3].x,vertices[node.v3].z,vertices[node.v2].x,vertices[node.v2].z));*/
					
					float dist2 = (node.position-pos).sqrMagnitude;
					if (minDist2 == -1 || dist2 < minDist2) {
						minDist2 = dist2;
						minNode2 = node;
					}
					
					if (constraint.Suitable (node)) {
						if (minConstDist2 == -1 || dist2 < minConstDist2) {
							minConstDist2 = dist2;
							minNodeConst2 = node;
						}
					}
					
					continue;
				}
				
				
				int dist = Mathfx.Abs (node.position.y-pos.y);
				
				if (minDist == -1 || dist < minDist) {
					minDist = dist;
					minNode = node;
				}
				
				if (constraint.Suitable (node)) {
					if (minConstDist == -1 || dist < minConstDist) {
						minConstDist = dist;
						minNodeConst = node;
					}
				}
			}
			
			NNInfo nninfo = new NNInfo (minNode == null ? minNode2 : minNode, minNode == null ? NearestNodePriority.Low : NearestNodePriority.High);
			
			//Find the point closest to the nearest triangle
			//if (minNode == null) {
				
			if (nninfo.node != null) {
				MeshNode node = nninfo.node as MeshNode;//minNode2 as MeshNode;
				
				Vector3[] triangle = new Vector3[3] {vertices[node.v1],vertices[node.v2],vertices[node.v3]};
				Vector3 clP = Polygon.ClosesPointOnTriangle (triangle,position);
				
				nninfo.clampedPosition = clP;
			}
			
			nninfo.constrainedNode = minNodeConst == null ? minNodeConst2 : minNodeConst;
			
			if (nninfo.constrainedNode != null) {
				MeshNode node = nninfo.constrainedNode as MeshNode;//minNode2 as MeshNode;
				
				Vector3[] triangle = new Vector3[3] {vertices[node.v1],vertices[node.v2],vertices[node.v3]};
				Vector3 clP = Polygon.ClosesPointOnTriangle (triangle,position);
				
				nninfo.constClampedPosition = clP;
			}
			
			return nninfo;
		}
		
		public void BuildFunnelCorridor (Node[] path, int startIndex, int endIndex, List<Vector3> left, List<Vector3> right) {
			BuildFunnelCorridor (this,path,startIndex,endIndex,left,right);
		}
		
		public static void BuildFunnelCorridor (INavmesh graph, Node[] path, int startIndex, int endIndex, List<Vector3> left, List<Vector3> right) {
			
			if (graph == null) {
				Debug.LogError ("Couldn't cast graph to the appropriate type (graph isn't a Navmesh type graph, it doesn't implement the INavmesh interface)");
				return;
			}
			
			Int3[] vertices = graph.vertices;
			
			int lastLeftIndex = -1;
			int lastRightIndex = -1;
			
			for (int i=startIndex;i<endIndex;i++) {
				//Find the connection between the nodes
				
				MeshNode n1 = path[i] as MeshNode;
				MeshNode n2 = path[i+1] as MeshNode;
				
				bool foundFirst = false;
				
				int first = -1;
				int second = -1;
				
				for (int x=0;x<3;x++) {
					//Vector3 vertice1 = vertices[n1.vertices[x]];
					int vertice1 = n1.GetVertexIndex (x);
					for (int y=0;y<3;y++) {
						//Vector3 vertice2 = vertices[n2.vertices[y]];
						int vertice2 = n2.GetVertexIndex (y);
						
						if (vertice1 == vertice2) {
							if (foundFirst) {
								second = vertice2;
								break;
							} else {
								first = vertice2;
								foundFirst = true;
							}
						}
					}
				}
				
				if (first == -1 || second == -1) {
					left.Add (n1.position);
					right.Add (n1.position);
					left.Add (n2.position);
					right.Add (n2.position);
					lastLeftIndex = first;
					lastRightIndex = second;
					
				} else {
				
					//Debug.DrawLine ((Vector3)vertices[first]+Vector3.up*0.1F,(Vector3)vertices[second]+Vector3.up*0.1F,Color.cyan);
					//Debug.Log (first+" "+second);
					if (first == lastLeftIndex) {
						left.Add (vertices[first]);
						right.Add (vertices[second]);
						lastLeftIndex = first;
						lastRightIndex = second;
						
					} else if (first == lastRightIndex) {
						left.Add (vertices[second]);
						right.Add (vertices[first]);
						lastLeftIndex = second;
						lastRightIndex = first;
						
					} else if (second == lastLeftIndex) {
						left.Add (vertices[second]);
						right.Add (vertices[first]);
						lastLeftIndex = second;
						lastRightIndex = first;
						
					} else {
						left.Add (vertices[first]);
						right.Add (vertices[second]);
						lastLeftIndex = first;
						lastRightIndex = second;
					}
				}
			}
		}
		
		public void AddPortal (Node n1, Node n2, List<Vector3> left, List<Vector3> right) {
		}
		
		
		public void UpdateArea (GraphUpdateObject o) {
			
		}
		
		public static void UpdateArea (GraphUpdateObject o, NavGraph graph) {
			
			INavmesh navgraph = graph as INavmesh;
			
			if (navgraph == null) { Debug.LogError ("Update Area on NavMesh must be called with a graph implementing INavmesh"); return; }
			
			if (graph.nodes == null || graph.nodes.Length == 0) {
				Debug.LogError ("NavGraph hasn't been generated yet or does not contain any nodes");
				return;// new NNInfo ();
			}
			
			//System.DateTime startTime = System.DateTime.Now;
				
			Bounds bounds = o.bounds;
			
			Rect r = Rect.MinMaxRect (bounds.min.x,bounds.min.z,bounds.max.x,bounds.max.z);
			
			Vector3 a = new Vector3 (r.xMin,0,r.yMin);//	-1 	-1
			Vector3 b = new Vector3 (r.xMin,0,r.yMax);//	-1	 1 
			Vector3 c = new Vector3 (r.xMax,0,r.yMin);//	 1 	-1
			Vector3 d = new Vector3 (r.xMax,0,r.yMax);//	 1 	 1
			
			
			for (int i=0;i<graph.nodes.Length;i++) {
				MeshNode node = graph.nodes[i] as MeshNode;
				
				bool inside = false;
				
				int allLeft = 0;
				int allRight = 0;
				int allTop = 0;
				int allBottom = 0;
				
				for (int v=0;v<3;v++) {
					
					Vector3 vert = (Vector3)navgraph.vertices[node[v]];
					Vector2 vert2D = new Vector2 (vert.x,vert.z);
					
					if (r.Contains (vert2D)) {
						//Debug.DrawRay (vert,Vector3.up,Color.yellow);
						inside = true;
						break;
					}
					
					if (vert.x < r.xMin) allLeft++;
					if (vert.x > r.xMax) allRight++;
					if (vert.z < r.yMin) allTop++;
					if (vert.z > r.yMax) allBottom++;
					
					//if (!bounds.Contains (node[v]) {
					//	inside = false;
					//	break;
					//}
				}
				if (!inside) {
					if (allLeft == 3 || allRight == 3 || allTop == 3 || allBottom == 3) {
						continue;
					}
				}
				
				for (int v=0;v<3;v++) {
					int v2 = v > 1 ? 0 : v+1;
					
					Vector3 vert1 = (Vector3)navgraph.vertices[node[v]];
					Vector3 vert2 = (Vector3)navgraph.vertices[node[v2]];
					
					if (Polygon.Intersects (a,b,vert1,vert2)) { inside = true; break; }
					if (Polygon.Intersects (a,c,vert1,vert2)) { inside = true; break; }
					if (Polygon.Intersects (c,d,vert1,vert2)) { inside = true; break; }
					if (Polygon.Intersects (d,b,vert1,vert2)) { inside = true; break; }
				}
				
				
				
				if (!inside && ContainsPoint (node,a,navgraph.vertices)) { inside = true; }//Debug.DrawRay (a+Vector3.right*0.01F*i,Vector3.up,Color.red); }
				if (!inside && ContainsPoint (node,b,navgraph.vertices)) { inside = true; } //Debug.DrawRay (b+Vector3.right*0.01F*i,Vector3.up,Color.red); }
				if (!inside && ContainsPoint (node,c,navgraph.vertices)) { inside = true; }//Debug.DrawRay (c+Vector3.right*0.01F*i,Vector3.up,Color.red); }
				if (!inside && ContainsPoint (node,d,navgraph.vertices)) { inside = true; }//Debug.DrawRay (d+Vector3.right*0.01F*i,Vector3.up,Color.red); }
				
				if (!inside) {
					continue;
				}
				
				o.WillUpdateNode(node);
				o.Apply (node);
				//Debug.DrawLine (vertices[node.v1],vertices[node.v2],Color.blue);
				//Debug.DrawLine (vertices[node.v2],vertices[node.v3],Color.blue);
				//Debug.DrawLine (vertices[node.v3],vertices[node.v1],Color.blue);
				//Debug.Break ();
			}
			
			//System.DateTime endTime = System.DateTime.Now;
			//float theTime = (endTime-startTime).Ticks*0.0001F;
			//Debug.Log ("Intersecting bounds with navmesh took "+theTime.ToString ("0.000")+" ms");
		
		}
		
		/** Returns the closest point of the node */
		public static Vector3 ClosestPointOnNode (MeshNode node, Int3[] vertices, Vector3 pos) {
			return Polygon.ClosesPointOnTriangle (vertices[node[0]],vertices[node[1]],vertices[node[2]],pos);
		}
		
		/** Returns if the point is inside the node in XZ space */
		public bool ContainsPoint (MeshNode node, Vector3 pos) {
			if (Polygon.IsClockwise ((Vector3)vertices[node.v1],(Vector3)vertices[node.v2], pos) && Polygon.IsClockwise ((Vector3)vertices[node.v2],(Vector3)vertices[node.v3], pos) && Polygon.IsClockwise ((Vector3)vertices[node.v3],(Vector3)vertices[node.v1], pos)) {
				return true;
			}
			return false;
		}
		
		/** Returns if the point is inside the node in XZ space */
		public static bool ContainsPoint (MeshNode node, Vector3 pos, Int3[] vertices) {
			if (Polygon.IsClockwiseMargin ((Vector3)vertices[node.v1],(Vector3)vertices[node.v2], pos) && Polygon.IsClockwiseMargin ((Vector3)vertices[node.v2],(Vector3)vertices[node.v3], pos) && Polygon.IsClockwiseMargin ((Vector3)vertices[node.v3],(Vector3)vertices[node.v1], pos)) {
				return true;
			}
			return false;
		}
		
		/** Scanns the graph using the path to an .obj mesh */
		public void Scan (string objMeshPath) {
			
			Mesh mesh = ObjImporter.ImportFile (objMeshPath);
			
			if (mesh == null) {
				Debug.LogError ("Couldn't read .obj file at '"+objMeshPath+"'");
				return;
			}
			
			sourceMesh = mesh;
			Scan ();
		}
		
		public override void Scan () {
			
			if (sourceMesh == null) {
				return;
			}
			
			GenerateMatrix ();
			
			//float startTime = 0;//Time.realtimeSinceStartup;
			
			Vector3[] vectorVertices = sourceMesh.vertices;
			
			triangles = sourceMesh.triangles;
			
			GenerateNodes (this,vectorVertices,triangles, out originalVertices, out _vertices);
		
		}
		
		/** Generates a navmesh. Based on the supplied vertices and triangles. Memory usage is about O(n) */
		public static void GenerateNodes (NavGraph graph, Vector3[] vectorVertices, int[] triangles, out Vector3[] originalVertices, out Int3[] vertices) {
			
			if (!(graph is INavmesh)) {
				Debug.LogError ("The specified graph does not implement interface 'INavmesh'");
				originalVertices = vectorVertices;
				vertices = new Int3[0];
				graph.nodes = graph.CreateNodes (0);
				return;
			}
			
			if (vectorVertices.Length == 0 || triangles.Length == 0) {
				originalVertices = vectorVertices;
				vertices = new Int3[0];
				graph.nodes = graph.CreateNodes (0);
				return;
			}
			
			vertices = new Int3[vectorVertices.Length];
			
			//Backup the original vertices
			//for (int i=0;i<vectorVertices.Length;i++) {
			//	vectorVertices[i] = graph.matrix.MultiplyPoint (vectorVertices[i]);
			//}
			
			int c = 0;
			/*int maxX = 0;
			int maxZ = 0;
			
			//Almost infinity
			int minX = 0xFFFFFFF;
			int minZ = 0xFFFFFFF;*/
			
			for (int i=0;i<vertices.Length;i++) {
				vertices[i] = (Int3)graph.matrix.MultiplyPoint (vectorVertices[i]);
				/*maxX = Mathfx.Max (vertices[i].x, maxX);
				maxZ = Mathfx.Max (vertices[i].z, maxZ);
				minX = Mathfx.Min (vertices[i].x, minX);
				minZ = Mathfx.Min (vertices[i].z, minZ);*/
			}
			
			//maxX = maxX-minX;
			//maxZ = maxZ-minZ;
			
			Dictionary<Int3,int> hashedVerts = new Dictionary<Int3,int> ();
			
			int[] newVertices = new int[vertices.Length];
				
			for (int i=0;i<vertices.Length-1;i++) {
				
				//int hash = Mathfx.ComputeVertexHash (vertices[i].x,vertices[i].y,vertices[i].z);
				
				//(vertices[i].x-minX)+(vertices[i].z-minX)*maxX+vertices[i].y*maxX*maxZ;
				//if (sortedVertices[i] != sortedVertices[i+1]) {
				if (!hashedVerts.ContainsKey (vertices[i])) {
					newVertices[c] = i;
					hashedVerts.Add (vertices[i], c);
					c++;
				}// else {
					//Debug.Log ("Hash Duplicate "+hash+" "+vertices[i].ToString ());
				//}
			}
			
			newVertices[c] = vertices.Length-1;
			
			//int hash2 = (newVertices[c].x-minX)+(newVertices[c].z-minX)*maxX+newVertices[c].y*maxX*maxZ;
			//int hash2 = Mathfx.ComputeVertexHash (newVertices[c].x,newVertices[c].y,newVertices[c].z);
			if (!hashedVerts.ContainsKey (vertices[newVertices[c]])) {
				
				hashedVerts.Add (vertices[newVertices[c]], c);
				c++;
			}
			
			for (int x=0;x<triangles.Length;x++) {
				Int3 vertex = vertices[triangles[x]];
				
				//int hash3 = (vertex.x-minX)+(vertex.z-minX)*maxX+vertex.y*maxX*maxZ;
				//int hash3 = Mathfx.ComputeVertexHash (vertex.x,vertex.y,vertex.z);
				//for (int y=0;y<newVertices.Length;y++) {
				triangles[x] = hashedVerts[vertex];
			}
			
			/*for (int i=0;i<triangles.Length;i += 3) {
				
				Vector3 offset = Vector3.forward*i*0.01F;
				Debug.DrawLine (newVertices[triangles[i]]+offset,newVertices[triangles[i+1]]+offset,Color.blue);
				Debug.DrawLine (newVertices[triangles[i+1]]+offset,newVertices[triangles[i+2]]+offset,Color.blue);
				Debug.DrawLine (newVertices[triangles[i+2]]+offset,newVertices[triangles[i]]+offset,Color.blue);
			}*/
			
			//Debug.Log ("NavMesh - Old vertice count "+vertices.Length+", new vertice count "+c+" "+maxX+" "+maxZ+" "+maxX*maxZ);
			
			Int3[] totalIntVertices = vertices;
			vertices = new Int3[c];
			originalVertices = new Vector3[c];
			for (int i=0;i<c;i++) {
				
				vertices[i] = totalIntVertices[newVertices[i]];//(Int3)graph.matrix.MultiplyPoint (vectorVertices[i]);
				originalVertices[i] = vertices[i];//vectorVertices[newVertices[i]];
			}
			
			Node[] nodes = graph.CreateNodes (triangles.Length/3);//new Node[triangles.Length/3];
			graph.nodes = nodes;
			for (int i=0;i<nodes.Length;i++) {
				
				MeshNode node = (MeshNode)nodes[i];//new MeshNode ();
				node.walkable = true;
				
				node.position = (vertices[triangles[i*3]] + vertices[triangles[i*3+1]] + vertices[triangles[i*3+2]])/3F;
				
				node.v1 = triangles[i*3];
				node.v2 = triangles[i*3+1];
				node.v3 = triangles[i*3+2];
				
				if (!Polygon.IsClockwise (vertices[node.v1],vertices[node.v2],vertices[node.v3])) {
					//Debug.DrawLine (vertices[node.v1],vertices[node.v2],Color.red);
					//Debug.DrawLine (vertices[node.v2],vertices[node.v3],Color.red);
					//Debug.DrawLine (vertices[node.v3],vertices[node.v1],Color.red);
					
					int tmp = node.v1;
					node.v1 = node.v3;
					node.v3 = tmp;
				}
				
				if (Polygon.IsColinear (vertices[node.v1],vertices[node.v2],vertices[node.v3])) {
					Debug.DrawLine (vertices[node.v1],vertices[node.v2],Color.red);
					Debug.DrawLine (vertices[node.v2],vertices[node.v3],Color.red);
					Debug.DrawLine (vertices[node.v3],vertices[node.v1],Color.red);
				}
				
				nodes[i] = node;
			}
			
			List<Node> connections = new List<Node> ();
			List<int> connectionCosts = new List<int> ();
			
			int identicalError = 0;
			
			for (int i=0;i<triangles.Length;i+=3) {
				
				connections.Clear ();
				connectionCosts.Clear ();
				
				//Int3 indices = new Int3(triangles[i],triangles[i+1],triangles[i+2]);
				
				Node node = nodes[i/3];
				
				for (int x=0;x<triangles.Length;x+=3) {
					
					if (x == i) {
						continue;
					}
					
					int count = 0;
					if (triangles[x] 	== 	triangles[i]) { count++; }
					if (triangles[x+1]	== 	triangles[i]) { count++; }
					if (triangles[x+2] 	== 	triangles[i]) { count++; }
					if (triangles[x] 	== 	triangles[i+1]) { count++; }
					if (triangles[x+1] 	== 	triangles[i+1]) { count++; }
					if (triangles[x+2] 	== 	triangles[i+1]) { count++; }
					if (triangles[x] 	== 	triangles[i+2]) { count++; }
					if (triangles[x+1] 	== 	triangles[i+2]) { count++; }
					if (triangles[x+2] 	== 	triangles[i+2]) { count++; }
					
					if (count >= 3) {
						identicalError++;
						Debug.DrawLine (vertices[triangles[x]],vertices[triangles[x+1]],Color.red);
						Debug.DrawLine (vertices[triangles[x]],vertices[triangles[x+2]],Color.red);
						Debug.DrawLine (vertices[triangles[x+2]],vertices[triangles[x+1]],Color.red);
						
					}
					
					if (count == 2) {
						Node other = nodes[x/3];
						connections.Add (other);
						connectionCosts.Add (Mathf.RoundToInt ((node.position-other.position).magnitude));
					}
				}
				
				node.connections = connections.ToArray ();
				node.connectionCosts = connectionCosts.ToArray ();
			}
			
			if (identicalError > 0) {
				Debug.LogError ("One or more triangles are identical to other triangles, this is not a good thing to have in a navmesh\nIncreasing the scale of the mesh might help\nNumber of triangles with error: "+identicalError+"\n");
			}
			RebuildBBTree (graph);
			
			//Debug.Log ("Graph Generation - NavMesh - Time to compute graph "+((Time.realtimeSinceStartup-startTime)*1000F).ToString ("0")+"ms");
		}
		
		/** Rebuilds the BBTree on a NavGraph.
		 * \astarpro
		 * \see NavMeshGraph::bbTree */
		public static void RebuildBBTree (NavGraph graph) {
			//BBTrees is a A* Pathfinding Project Pro only feature - The Pro version can be bought on the Unity Asset Store or on arongranberg.com
		}
		public void PostProcess () {
			int rnd = Random.Range (0,nodes.Length);
			
			Node nodex = nodes[rnd];
			
			NavGraph gr = null;
			
			if (AstarPath.active.astarData.GetGraphIndex(this) == 0) {
				gr = AstarPath.active.graphs[1];
			} else {
				gr = AstarPath.active.graphs[0];
			}
			
			rnd = Random.Range (0,gr.nodes.Length);
			
			List<Node> connections = new List<Node> ();
			List<int> connectionCosts = new List<int> ();
			
			connections.AddRange (nodex.connections);
			connectionCosts.AddRange (nodex.connectionCosts);
			
			Node otherNode = gr.nodes[rnd];
			
			connections.Add (otherNode);
			connectionCosts.Add (Mathf.RoundToInt ((nodex.position-otherNode.position).magnitude*100));
			
			nodex.connections = connections.ToArray ();
			nodex.connectionCosts = connectionCosts.ToArray ();
		}
		
		public void Sort (Vector3[] a) {
			
			bool changed = true;
		
			while (changed) {
				changed = false;
				for (int i=0;i<a.Length-1;i++) {
					if (a[i].x > a[i+1].x || (a[i].x == a[i+1].x && (a[i].y > a[i+1].y || (a[i].y == a[i+1].y && a[i].z > a[i+1].z)))) {
						Vector3 tmp = a[i];
						a[i] = a[i+1];
						a[i+1] = tmp;
						changed = true;
					}
				}
			}
		}
		
		public override void OnDrawGizmos (bool drawNodes) {
			
			if (!drawNodes) {
				return;
			}
			
			Matrix4x4 preMatrix = matrix;
			
			GenerateMatrix ();
			
			if (nodes == null) {
				Scan ();
			}
			
			if (nodes == null) {
				return;
			}
			
			if (preMatrix != matrix) {
				//Debug.Log ("Relocating Nodes");
				RelocateNodes (preMatrix, matrix);
			}
			
			for (int i=0;i<nodes.Length;i++) {
				
				
				MeshNode node = (MeshNode)nodes[i];
				
				Gizmos.color = NodeColor (node);//AstarColor.NodeConnection;
				
				if (node.walkable ) {
					
					if (node.parent != null) {
						Gizmos.DrawLine (node.position,node.parent.position);
					} else {
						for (int q=0;q<node.connections.Length;q++) {
							Gizmos.DrawLine (node.position,node.connections[q].position);
						}
					}
				
					Gizmos.color = AstarColor.MeshEdgeColor;
				} else {
					Gizmos.color = Color.red;
				}
				Gizmos.DrawLine (vertices[node.v1],vertices[node.v2]);
				Gizmos.DrawLine (vertices[node.v2],vertices[node.v3]);
				Gizmos.DrawLine (vertices[node.v3],vertices[node.v1]);
				
			}
			
		}
		
		//These functions are for serialization, the static ones are there so other graphs using mesh nodes can serialize them more easily
		public static void SerializeMeshNodes (INavmesh graph, Node[] nodes, AstarSerializer serializer) {
			
			System.IO.BinaryWriter stream = serializer.writerStream;
			
			for (int i=0;i<nodes.Length;i++) {
				MeshNode node = nodes[i] as MeshNode;
				
				if (node == null) {
					Debug.LogError ("Serialization Error : Couldn't cast the node to the appropriate type - NavMeshGenerator");
					return;
				}
				
				stream.Write (node.v1);
				stream.Write (node.v2);
				stream.Write (node.v3);
			}
			
			Int3[] vertices = graph.vertices;
			
			if (vertices == null) {
				vertices = new Int3[0];
			}
			
			stream.Write (vertices.Length);
			
			for (int i=0;i<vertices.Length;i++) {
				stream.Write (vertices[i].x);
				stream.Write (vertices[i].y);
				stream.Write (vertices[i].z);
			}
		}
		
		public static void DeSerializeMeshNodes (INavmesh graph, Node[] nodes, AstarSerializer serializer) {
			
			System.IO.BinaryReader stream = serializer.readerStream;
			
			for (int i=0;i<nodes.Length;i++) {
				MeshNode node = nodes[i] as MeshNode;
				
				if (node == null) {
					Debug.LogError ("Serialization Error : Couldn't cast the node to the appropriate type - NavMeshGenerator");
					return;
				}
				
				node.v1 = stream.ReadInt32 ();
				node.v2 = stream.ReadInt32 ();
				node.v3 = stream.ReadInt32 ();
			}
			
			int numVertices = stream.ReadInt32 ();
			
			graph.vertices = new Int3[numVertices];
			
			for (int i=0;i<numVertices;i++) {
				int x = stream.ReadInt32 ();
				int y = stream.ReadInt32 ();
				int z = stream.ReadInt32 ();
				
				graph.vertices[i] = new Int3 (x,y,z);
			}
				
			RebuildBBTree (graph as NavGraph);
		}
		
		public void SerializeNodes (Node[] nodes, AstarSerializer serializer) {
			NavMeshGraph.SerializeMeshNodes (this as INavmesh, nodes, serializer);
		}
		
		public void DeSerializeNodes (Node[] nodes, AstarSerializer serializer) {
			NavMeshGraph.DeSerializeMeshNodes (this as INavmesh, nodes, serializer);
		}
		
		public void SerializeSettings (AstarSerializer serializer) {
			
			System.IO.BinaryWriter stream = serializer.writerStream;
			
			serializer.AddValue ("offset",offset);
			serializer.AddValue ("rotation",rotation);
			serializer.AddValue ("scale",scale);
			
			if (sourceMesh != null) {
				
				Vector3[] verts = sourceMesh.vertices;
				int[] tris = sourceMesh.triangles;
				
				stream.Write (verts.Length);
				stream.Write (tris.Length);
				
				for (int i=0;i<verts.Length;i++) {
					stream.Write (verts[i].x);
					stream.Write (verts[i].y);
					stream.Write (verts[i].z);
				}
				
				for (int i=0;i<tris.Length;i++) {
					stream.Write (tris[i]);
				}
			} else {
				stream.Write (0);
				stream.Write (0);
			}
			
			serializer.AddUnityReferenceValue ("sourceMesh",sourceMesh);
		}
		
		public void DeSerializeSettings (AstarSerializer serializer) {
			
			System.IO.BinaryReader stream = serializer.readerStream;
			
			offset = (Vector3)serializer.GetValue ("offset",typeof(Vector3));
			rotation = (Vector3)serializer.GetValue ("rotation",typeof(Vector3));
			scale = (float)serializer.GetValue ("scale",typeof(float));
			
			GenerateMatrix ();
			
			Vector3[] verts = new Vector3[stream.ReadInt32 ()];
			int[] tris = new int[stream.ReadInt32 ()];
			
			for (int i=0;i<verts.Length;i++) {
				verts[i] = new Vector3(stream.ReadSingle (),stream.ReadSingle (),stream.ReadSingle ());
			}
				
			for (int i=0;i<tris.Length;i++) {
				tris[i] = stream.ReadInt32 ();
			}
			
			sourceMesh = serializer.GetUnityReferenceValue ("sourceMesh",typeof(Mesh)) as Mesh;
			
			if (Application.isPlaying) {
				sourceMesh = new Mesh ();
				sourceMesh.name = "NavGraph Mesh";
				sourceMesh.vertices = verts;
				sourceMesh.triangles = tris;
			}
		}
	}
	
	public class MeshNode : Node {
		//Vertices
		public int v1;
		public int v2;
		public int v3;
		
		public int GetVertexIndex (int i) {
			if (i == 0) {
				return v1;
			} else if (i == 1) {
				return v2;
			} else if (i == 2) {
				return v3;
			} else {
				throw new System.ArgumentOutOfRangeException ("A MeshNode only contains 3 vertices");
			}
		}
		
		public int this[int i]
	    {
	        get
	        {
	            return GetVertexIndex (i);
	        }
	    }
	    
	    public Vector3 ClosestPoint (Vector3 p, Int3[] vertices) {
	    	return Polygon.ClosesPointOnTriangle (vertices[v1],vertices[v2],vertices[v3],p);
	    }
	}
}