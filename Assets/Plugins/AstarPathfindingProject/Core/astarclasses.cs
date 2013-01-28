//#define ManhattanHeuristic	//"Heuristic"[NoHeuristic,ManhattanHeuristic,DiagonalManhattanHeuristic,EucledianHeuristic]Forces the heuristic to be the chosen one or disables it altogether
//#define NoVirtualUpdateH //Should UpdateH be virtual or not
//#define NoVirtualUpdateG //Should UpdateG be virtual or not
//#define NoVirtualOpen //Should Open be virtual or not
//#define NoHScaling    //Should H score scaling be enabled. H Score is usually multiplied with UpdateH's parameter 'scale'


using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;

namespace Pathfinding {
	/** Holds one node in a navgraph. A node is a simple object in the shape of a square (GridNode), triangle (NavMeshNode), or point (the rest).\n
	 * A node has a position and a list of neighbour nodes which are the nodes this node can form a straight path to\n
	 * Size: (4*3)+4+4+4+4+4+4+8+8*n = 44+8*n bytes where \a n is the number of connections (estimate) */
	public class Node {
	
		//Size = 4*3 + 4 + 4 + 4 + 4 + 4 + 4 = 28 bytes
		
		/** Position in world space of the node. The position is stored as integer coordinates to avoid precision loss when the node is far away from the world origin. The default precision is 0.01 (one centimeter). */
		public Int3 position;
		
		/** Previous node in the path. The path is traced from the end node to the start node by following the parent variables until the start node is reached
		 * \see Path::Trace */
		public Node parent;
		
		public int pathIDx; /**< The Path ID this node was last used by. \see Path::pathID */
		
		public int pathID {
			get {
				return pathIDx & 0xFFFF;
			}
			set {
				pathIDx = (pathIDx & ~0xFFFF) | value;
			}
		}
		
		//Not used anymore, only slowed down pathfinding with no other positive effect
		/*public int heapIndex {
			get {
				return pathIDx >> 16;
			}
			set {
				pathIDx = (pathIDx & 0xFFFF) | (value << 16);
			}
		}*/
		
		public static Path activePath; /**< Path which is currently being calculated */
		
		public int g; /**< G score. The total cost of moving from the start node to this node
		\todo #g should be stored as uint or perhaps ushort if performance is not damaged */
		public int h; /**< H score. Estimated cost of moving from this node to the end node */
		
		public int cost; /**< The cost for moving from #parent to this node.
		\todo #cost should be stored as uint or perhaps ushort if performance is not damaged */
		public int penalty; /**< Penlty cost for walking on this node. This can be used to make it "harder" to walk over certain areas.
		\todo #penalty should be an uint or even better, ushort */
		
		public int tags = 1; /**< Tags for walkability */
		
		/** List of all connections from this node. This node's neighbour nodes are stored in this array.\n
		 * \note Not all connections are stored in this array, some node types (such as Pathfinding::GridNode) use custom connection systems which they store somewhere else.
		 * \see #connectionCosts */
		public Node[] connections;
		
		/** Cost for the connections to other nodes. The cost of moving from this node to connections[x] is stored in connectionCosts[x].
		 * \see #connections */
		public int[] connectionCosts;
		
#region Flags
		
		//Last 8 bytes used (area = last 8, walkable = next 1, graphIndex = 18 - 22), can be used for other stuff
		
		/** Values for Area, walkability and graph index. \image html nodeFlags.png
		  * \see walkable
		  * \see area
		  * \see graphIndex
		  */
		public int flags;
		
		const int WalkableBitNumber = 23; /**< Bit number for the #walkable bool */
		const int WalkableBit = 1 << WalkableBitNumber; /** 1 \<\< #WalkableBitNumber */
		
		const int AreaBitNumber = 24; /**< Bit number at which #area starts */
		const int AreaBitsSize = 0xFF; /**< Size of the #area bits */
		const int NotAreaBits = ~(AreaBitsSize << AreaBitNumber); /**< The bits in #flags which are NOT #area bits */
		
		const int GraphIndexBitNumber = 18;
		const int GraphIndexBitsSize = 0x1F;
		const int NotGraphIndexBits = ~(GraphIndexBitsSize << GraphIndexBitNumber); /**< Bits which are NOT #graphIndex bits */

		/** Returns bit 8 from #flags. Used to flag special nodes with special pathfinders */
		public bool Bit8 {
			get {
				return (flags & 0x100) != 0;
			}
			set {
				flags = (flags & ~0x100) | (value ? 0x100 : 0);
			}
		}
		
		/** Is the node walkable */
		public bool walkable {
			get {
				//return ((flags >> 23) & 1) == 1;
				return (flags & WalkableBit) == WalkableBit;
			}
			set {
				flags = (flags & ~WalkableBit) | (value ? WalkableBit : 0);
			}
		}
		
		/** Area ID of the node. Nodes which there are no valid path between have different area values.
		  * \note Small areas can have have the same area ID since only 256 ID values are available
		  * \see AstarPath::minAreaSize
		  */
		public int area {
			get {
				return (flags >> AreaBitNumber) & AreaBitsSize;
			}
			set {
				flags = (flags & NotAreaBits) | (value << AreaBitNumber);
			}
		}
				
		/** The index of the graph this node is in.
		  * \see \link Pathfinding::AstarData::graphs AstarData.graphs \endlink */
		public int graphIndex {
			get {
				return ((flags >> GraphIndexBitNumber) & GraphIndexBitsSize);
			}
			set {
				flags = (flags & NotGraphIndexBits) | ((value & GraphIndexBitsSize) << GraphIndexBitNumber);
			}
		}
		
#endregion
		
		/** F score. The F score is the #g score + #h score, that is the cost it taken to move to this node from the start + the estimated cost to move to the end node.\n
		 * Nodes are sorted by their F score, nodes with lower F scores are opened first */
		public int f {
			get {
				return g+h;
			}
		}
		
		public 
		virtual
		void UpdateH (Int3 targetPosition, Heuristic heuristic, float scale) {		
			//Choose the correct heuristic, compute it and store it in the \a h variable
			if (heuristic == Heuristic.None) {
				h = 0;
				return;
			}
			
			if (heuristic == Heuristic.Manhattan) {
				h = Mathfx.RoundToInt  (
				                      (Abs (position.x-targetPosition.x) + 
				                      Abs (position.y-targetPosition.y) + 
				                      Abs (position.z-targetPosition.z))
				                      * scale
				                      );
			} else if (heuristic == Heuristic.DiagonalManhattan) {
				int xDistance = Abs (position.x-targetPosition.x);
				int zDistance = Abs (position.z-targetPosition.z);
				if (xDistance > zDistance) {
				     h = (14*zDistance + 10*(xDistance-zDistance))/10;
				} else {
				     h = (14*xDistance + 10*(zDistance-xDistance))/10;
				}
				h = Mathfx.RoundToInt (h * scale);
			} else {
				h = Mathfx.RoundToInt ((position-targetPosition).magnitude*scale);
			}
		}
		
		/** Implementation of the Absolute function */
		public static int Abs (int x) {
			return x < 0 ? -x : x;
		}
		
		/** Calculates the G score. The G score is the cost of moving from the start node to this node, including all penalties.\n \code g = parent.g+cost+penalty \endcode.
		 * \see #parent
		 * \see #cost
		 * \see #penalty */
		public
		virtual
		void UpdateG () {
			g = parent.g+cost+penalty;
			//f = g+h;
		}
		
		/** Updates G score for this node and nodes which have this node set as #parent */
		public 
	virtual
	void UpdateAllG (BinaryHeap open) {
			BaseUpdateAllG (open);
		}
		
		/** Updates G score for this node and nodes which have this node set as #parent. This is to allow inheritance in higher levels than one, you can't call base.base in virtual functions */
		public void BaseUpdateAllG (BinaryHeap open) {
			g = parent.g+cost+penalty;
			
			open.Add (this);
			
			if (connections == null) {
				return;
			}
			
			//Loop through the connections of this node and call UpdateALlG on nodes which have this node set as #parent and has been searched by the pathfinder for this path */
			for (int i=0;i<connections.Length;i++) {
				
				if (connections[i].parent == this && connections[i].pathID == pathID) {
					connections[i].UpdateAllG (open);
				}
			}
			
		}
		
		public virtual int[] InitialOpen (BinaryHeap open, Int3 targetPosition, Int3 position, Path path, bool doOpen) {
			return BaseInitialOpen (open,targetPosition,position,path,doOpen);
		}
		
		public int[] BaseInitialOpen (BinaryHeap open, Int3 targetPosition, Int3 position, Path path, bool doOpen) {
			
			if (connectionCosts == null) {
				return null;
			}
			
			int[] costs = connectionCosts;
			connectionCosts = new int[connectionCosts.Length];
			
			
			for (int i=0;i<connectionCosts.Length;i++) {
				connectionCosts[i] = (connections[i].position-position).costMagnitude;
			}
			
			if (!doOpen) {	
				for (int i=0;i<connectionCosts.Length;i++) {
					Node other = connections[i];
					for (int q = 0;q < other.connections.Length;q++) {
						if (other.connections[q] == this) {
							other.connectionCosts[q] = connectionCosts[i];
							break;
						}
					}
				}
			}
			
			//int[] tmp = connectionCosts;
			
			//Should we open the node and reset the distances after that or only calculate the distances and don't reset them
			if (doOpen) {
				Open (open,targetPosition,path);
				connectionCosts = costs;
				
				/*for (int i=0;i<connectionCosts.Length;i++) {
					for (int q = 0;q < connections[i].connections.Length;q++) {
						if (connections[i].connections[q] == this) {
							connections[i].connectionCosts[q] = connectionCosts[i];
							break;
						}
					}
				}*/
			}
			
			return costs;
		}
		
		/** Resets the costs modified by the InitialOpen function when 'doOpen' was false (for the end node). This is called at the end of a pathfinding search */
		public virtual void ResetCosts (int[] costs) {
			BaseResetCosts (costs);
		}
		
		/** \copydoc ResetCosts */
		public void BaseResetCosts (int[] costs) {
			connectionCosts = costs;
			
			if (connectionCosts == null) {
				return;
			}
			
			for (int i=0;i<connectionCosts.Length;i++) {
				Node other = connections[i];
				for (int q = 0;q < other.connections.Length;q++) {
					if (other.connections[q] == this) {
						other.connectionCosts[q] = connectionCosts[i];
						break;
					}
				}
			}
		}
		
		/** Opens the nodes connected to this node */
		public 
	virtual
	void Open (BinaryHeap open, Int3 targetPosition, Path path) {
			BaseOpen (open,targetPosition,path);
		}
		
		/** Opens the nodes connected to this node. This is a base call and can be called by node classes overriding the Open function to open all connections in the #connections array.
		 * \see #connections
		 * \see Open */
		public void BaseOpen (BinaryHeap open, Int3 targetPosition, Path path) {
			
			if (connections == null) {
				return;
			}
			
			for (int i=0;i<connections.Length;i++) {
				Node node = connections[i];
				
				if (!path.CanTraverse (node)) {
					continue;
				}
				
				if (node.pathID != pathID) {
					
					node.parent = this;
					node.pathID = pathID;
					
					node.cost = connectionCosts[i];
					
					node.UpdateH (targetPosition, path.heuristic, path.heuristicScale);
					node.UpdateG ();
					
					open.Add (node);
					
					//Debug.DrawLine (position,node.position,Color.cyan);
					//Debug.Log ("Opening	Node "+node.position.ToString ()+" "+g+" "+node.cost+" "+node.g+" "+node.f);
				} else {
					//If not we can test if the path from the current node to this one is a better one then the one already used
					int tmpCost = connectionCosts[i];//(current.costs == null || current.costs.Length == 0 ? costs[current.neighboursKeys[i]] : current.costs[current.neighboursKeys[i]]);
					
					//Debug.Log ("Trying	Node "+node.position.ToString ()+" "+(g+tmpCost+node.penalty)+" "+node.g+" "+node.f);
					//Debug.DrawLine (position,node.position,Color.yellow);
					if (g+tmpCost+node.penalty < node.g) {
						node.cost = tmpCost;
						//node.extraCost = extraCost2;
						node.parent = this;
						
						node.UpdateAllG (open);
						
						open.Add (node);
						
						//Debug.DrawLine (current.vectorPos,current.neighbours[i].vectorPos,Color.cyan); //Uncomment for @Debug
					}
					
					 else if (node.g+tmpCost+penalty < g) {//Or if the path from this node ("node") to the current ("current") is better
						bool contains = false;
						
						//Make sure we don't travel along the wrong direction of a one way link now, make sure the Current node can be moved to from the other Node.
						if (node.connections != null) {
							for (int y=0;y<node.connections.Length;y++) {
								if (node.connections[y] == this) {
									contains = true;
									break;
								}
							}
						}
						
						if (!contains) {
							continue;
						}
						
						parent = node;
						cost = tmpCost;
						//extraCost = extraCost2;
						
						UpdateAllG (open);
						//open.Add (this);
						//Debug.DrawLine (current.vectorPos,current.neighbours[i].vectorPos,Color.blue); //Uncomment for @Debug
						open.Add (this);
					}
				}
			}
		}
		
		/** Adds all connecting nodes to the \a stack and sets the #area variable to \a area */
		public virtual void FloodFill (Stack<Node> stack, int area) {
			BaseFloodFill (stack,area);
		}
		
		/** Adds all connecting nodes to the \a stack and sets the #area variable to \a area. This is a base function and can be called by node classes overriding the FloodFill function to add the connections in the #connections array */
		public void BaseFloodFill (Stack<Node> stack, int area) {
			
			if (connections == null) {
				return;
			}
			
			for (int i=0;i<connections.Length;i++) {
				if (connections[i].walkable && connections[i].area != area) {
					stack.Push (connections[i]);
					connections[i].area = area;
				}
			}
		}
		
		/** Remove connections to unwalkable nodes.
		 * This function loops through all connections and removes the ones which lead to unwalkable nodes.\n
		 * This can speed up performance if a lot of nodes have connections to unwalkable nodes, they usually don't though
		 * \note This function does not add connections which might have been removed previously
		*/
		public virtual void UpdateConnections () {
			
			if (connections != null) {
				List<Node> newConn = null;
				List<int> newCosts = null;
			
				for (int i=0;i<connections.Length;i++) {
					if (!connections[i].walkable) {
						
						if (newConn == null) {
							newConn = new List<Node> (connections.Length-1);
							newCosts = new List<int> (connections.Length-1);
							for (int j=0;j<i;j++) {
								newConn.Add (connections[j]);
								newCosts.Add (connectionCosts[j]);
							}
						}
					} else if (newConn != null) {
						newConn.Add (connections[i]);
						newCosts.Add (connectionCosts[i]);
					}
				}
			}
			
		}
		
		/** Calls UpdateConnections on all neighbours.
		 * Neighbours are all nodes in the connections array. Good to use if the node has been set to unwalkable-
		 * \see UpdateConnections */
		public virtual void UpdateNeighbourConnections () {
			if (connections != null) {
				for (int i=0;i<connections.Length;i++) {
					connections[i].UpdateConnections ();
				}
			}
		}
		
		/** Returns true if this node has a connection to the node.
		 * \note this might not return true for node classes using their own connection system (like GridNode)
		*/
		public virtual bool ContainsConnection (Node node) {
			if (connections != null) {
				for (int i=0;i<connections.Length;i++) {
					if (connections[i] == node) {
						return true;
					}
				}
			}
			return false;
		}
		
		/** Add a connection to the node with the specified cost
		 * \note This will create a one-way connection, consider calling the same function on the other node too 
		 * \see RemoveConnection
		 * \see Pathfinding::Int3::costMagnitude */
		public void AddConnection (Node node, int cost) {
			
			if (connections == null) {
				connections = new Node[0];
				connectionCosts = new int[0];
			} else {
				for (int i=0;i<connections.Length;i++) {
					//Connection already exists
					if (connections[i] == node) {
						//Just update cost
						connectionCosts[i] = cost;
						return;
					}
				}
			}
			
			Node[] old_connections = connections;
			int[] old_costs = connectionCosts;
			
			connections = new Node[connections.Length+1];
			connectionCosts = new int[connections.Length];
			
			for (int i=0;i<old_connections.Length;i++) {
				connections[i] = old_connections[i];
				connectionCosts[i] = old_costs[i];
			}
			
			connections[old_connections.Length] = node;
			connectionCosts[old_connections.Length] = cost;
		}
		
		/** Removes the connection to the node if it exists.
		 * Returns true if a connection was removed, returns false if no connection to the node was found
		 * \note This will only remove the connection from this node to \a node, but it will still exist in the other direction
		 * consider calling the same function on the other node too
		 * \see AddConnection
		 */
		public virtual bool RemoveConnection (Node node) {
			if (connections == null) { return false; }
			
			for (int i=0;i<connections.Length;i++) {
				if (connections[i] == node) {
					//Swap with last item
					connections[i] = connections[connections.Length-1];
					connectionCosts[i] = connectionCosts[connectionCosts.Length-1];
					
					//Create new arrays
					Node[] new_connections = new Node[connections.Length-1];
					int[] new_costs = new int[connections.Length-1];
					
					//Copy the remaining connections
					for (int j=0;j<connections.Length-1;j++) {
						new_connections[j] = connections[j];
						new_costs[j] = connectionCosts[j];
					}
					
					connections = new_connections;
					connectionCosts = new_costs;
					//Debug.Log ("Done Remove");
					return true;
				}
			}
			return false;
		}
		
	}
	
	/** A class for holding a user placed connection */
	public class UserConnection {
		
		public Vector3 p1;
		public Vector3 p2;
		public ConnectionType type;
		
		//Connection
		public bool doOverrideCost = false;
		public int overrideCost = 0;
		
		public bool oneWay = false;
		public bool enable = true;
		public float width = 0;
		
		//Modify Node
		public bool doOverrideWalkability = true;
		public int overridePenalty = 0;
		public bool doOverridePenalty = false;
		
	}
	
	/** Holds a coordinate in integers */
	public struct Int3 {
		public int x;
		public int y;
		public int z;
		
		//These should be set to the same value (only PrecisionFactor should be 1 divided by Precision)
		public const int Precision = 100;
		public const float FloatPrecision = 100F;
		public const float PrecisionFactor = 0.01F;
		
		public Int3 (Vector3 position) {
			x = (int)System.Math.Round (position.x*FloatPrecision);
			y = (int)System.Math.Round (position.y*FloatPrecision);
			z = (int)System.Math.Round (position.z*FloatPrecision);
			//x = Mathf.RoundToInt (position.x);
			//y = Mathf.RoundToInt (position.y);
			//z = Mathf.RoundToInt (position.z);
		}
		
		
		public Int3 (int _x, int _y, int _z) {
			x = _x;
			y = _y;
			z = _z;
		}
		
		public static bool operator == (Int3 lhs, Int3 rhs) {
			return 	lhs.x == rhs.x &&
					lhs.y == rhs.y &&
					lhs.z == rhs.z;
		}
		
		public static bool operator != (Int3 lhs, Int3 rhs) {
			return 	lhs.x != rhs.x ||
					lhs.y != rhs.y ||
					lhs.z != rhs.z;
		}
		
		public static implicit operator Int3 (Vector3 ob) {
			return new Int3 (
				(int)System.Math.Round (ob.x*FloatPrecision),
				(int)System.Math.Round (ob.y*FloatPrecision),
				(int)System.Math.Round (ob.z*FloatPrecision)
				);
			//return new Int3 (Mathf.RoundToInt (ob.x*FloatPrecision),Mathf.RoundToInt (ob.y*FloatPrecision),Mathf.RoundToInt (ob.z*FloatPrecision));
		}
		
		public static implicit operator Vector3 (Int3 ob) {
			return new Vector3 (ob.x*PrecisionFactor,ob.y*PrecisionFactor,ob.z*PrecisionFactor);
		}
		
		public static Int3 operator - (Int3 lhs, Int3 rhs) {
			lhs.x -= rhs.x;
			lhs.y -= rhs.y;
			lhs.z -= rhs.z;
			return lhs;
		}
		
		public static Int3 operator + (Int3 lhs, Int3 rhs) {
			lhs.x += rhs.x;
			lhs.y += rhs.y;
			lhs.z += rhs.z;
			return lhs;
		}
		
		public static Int3 operator * (Int3 lhs, int rhs) {
			lhs.x *= rhs;
			lhs.y *= rhs;
			lhs.z *= rhs;
			
			return lhs;
		}
		
		public static Int3 operator * (Int3 lhs, float rhs) {
			lhs.x = (int)System.Math.Round (lhs.x * rhs);
			lhs.y = (int)System.Math.Round (lhs.y * rhs);
			lhs.z = (int)System.Math.Round (lhs.z * rhs);
			
			return lhs;
		}
		
		public static Int3 operator * (Int3 lhs, Vector3 rhs) {
			lhs.x = (int)System.Math.Round (lhs.x * rhs.x);
			lhs.y =	(int)System.Math.Round (lhs.y * rhs.y);
			lhs.z = (int)System.Math.Round (lhs.z * rhs.z);
			
			return lhs;
		}
		
		public static Int3 operator / (Int3 lhs, float rhs) {
			lhs.x = (int)System.Math.Round (lhs.x / rhs);
			lhs.y = (int)System.Math.Round (lhs.y / rhs);
			lhs.z = (int)System.Math.Round (lhs.z / rhs);
			return lhs;
		}
		
		public int this[int i] {
			get {
				return i == 0 ? x : (i == 1 ? y : z);
			}
		}
		
		public static int Dot (Int3 lhs, Int3 rhs) {
			return
					lhs.x * rhs.x +
					lhs.y * rhs.y +
					lhs.z * rhs.z;
		}
		
		public Int3 NormalizeTo (int newMagn) {
			float magn = magnitude;
			
			if (magn == 0) {
				return this;
			}
			
			x *= newMagn;
			y *= newMagn;
			z *= newMagn;
			
			x = (int)System.Math.Round (x/magn);
			y = (int)System.Math.Round (y/magn);
			z = (int)System.Math.Round (z/magn);
			
			return this;
		}
		
		/** Returns the magnitude of the vector. The magnitude is the 'length' of the vector from 0,0,0 to this point. Can be used for distance calculations:
		  * \code Debug.Log ("Distance between 3,4,5 and 6,7,8 is: "+(new Int3(3,4,5) - new Int3(6,7,8)).magnitude); \endcode
		  */
		public float magnitude {
			get {
				//It turns out that using doubles is just as fast as using ints with Mathf.Sqrt. And this can also handle larger numbers (possibly with small errors when using huge numbers)!
				
				double _x = x;
				double _y = y;
				double _z = z;
				
				return (float)System.Math.Sqrt (_x*_x+_y*_y+_z*_z);
				
				//return Mathf.Sqrt (x*x+y*y+z*z);
			}
		}
		
		/** Magnitude used for the cost between two nodes. The default cost between two nodes can be calculated like this:
		  * \code int cost = (node1.position-node2.position).costMagnitude; \endcode
		  */
		public int costMagnitude {
			get {
				return (int)System.Math.Round (magnitude);
			}
		}
		
		/** The magnitude in world units */
		public float worldMagnitude {
			get {
				double _x = x;
				double _y = y;
				double _z = z;
				
				return (float)System.Math.Sqrt (_x*_x+_y*_y+_z*_z)*PrecisionFactor;
				
				//Scale numbers down
				/*float _x = x*PrecisionFactor;
				float _y = y*PrecisionFactor;
				float _z = z*PrecisionFactor;
				return Mathf.Sqrt (_x*_x+_y*_y+_z*_z);*/
			}
		}
		
		/** The squared magnitude of the vector */
		public float sqrMagnitude {
			get {
				double _x = x;
				double _y = y;
				double _z = z;
				return (float)(_x*_x+_y*_y+_z*_z);
				//return x*x+y*y+z*z;
			}
		}
		
		/** \warning Can cause number overflows if the magnitude is too large */
		public int unsafeSqrMagnitude {
			get {
				return x*x+y*y+z*z;
			}
		}
		
		/** To avoid number overflows. \deprecated #magnitude now uses the same implementation */
		[System.Obsolete ("Same implementation as .magnitude")]
		public float safeMagnitude {
			get {
				//Of some reason, it is faster to use doubles (almost 40% faster)
				double _x = x;
				double _y = y;
				double _z = z;
				
				return (float)System.Math.Sqrt (_x*_x+_y*_y+_z*_z);
				
				//Scale numbers down
				/*float _x = x*PrecisionFactor;
				float _y = y*PrecisionFactor;
				float _z = z*PrecisionFactor;
				//Find the root and scale it up again
				return Mathf.Sqrt (_x*_x+_y*_y+_z*_z)*FloatPrecision;*/
			}
		}
		
		/** To avoid number overflows. The returned value is the squared magnitude of the world distance (i.e divided by Precision) 
		 * \deprecated .sqrMagnitude is now per default safe (#unsafeSqrMagnitude can be used for unsafe operations) */
		[System.Obsolete (".sqrMagnitude is now per default safe (.unsafeSqrMagnitude can be used for unsafe operations)")]
		public float safeSqrMagnitude {
			get {
				float _x = x*PrecisionFactor;
				float _y = y*PrecisionFactor;
				float _z = z*PrecisionFactor;
				return _x*_x+_y*_y+_z*_z;
			}
		}
		
		public static implicit operator string (Int3 ob) {
			return ob.ToString ();
		}
		
		/** Returns a nicely formatted string representing the vector */
		public override string ToString () {
			return "( "+x+", "+y+", "+z+")";
		}
		
		public override bool Equals (System.Object o) {
			
			if (o == null) return false;
			
			Int3 rhs = (Int3)o;
			
			return 	x == rhs.x &&
					y == rhs.y &&
					z == rhs.z;
		}
		
		public override int GetHashCode () {
			return x*9+y*10+z*11;
		}
	}
	
	[System.Serializable]
	/** Stores editor colors */
	public class AstarColor {
		
		public Color _NodeConnection;
		public Color _UnwalkableNode;
		public Color _BoundsHandles;
	
		public Color _ConnectionLowLerp;
		public Color _ConnectionHighLerp;
		
		public Color _MeshEdgeColor;
		public Color _MeshColor;
		
		/** Holds user set area colors.
		 * Use GetAreaColor to get an area color */
		public Color[] _AreaColors;
		
		public static Color NodeConnection = new Color (0,1,0,0.5F);
		public static Color UnwalkableNode = new Color (1,0,0,0.5F);
		public static Color BoundsHandles = new Color (0.29F,0.454F,0.741F,0.9F);
		
		public static Color ConnectionLowLerp = new Color (0,1,0,0.5F);
		public static Color ConnectionHighLerp = new Color (1,0,0,0.5F);
		
		public static Color MeshEdgeColor = new Color (0,0,0,0.5F);
		public static Color MeshColor = new Color (0,0,0,0.5F);
		
		/** Holds user set area colors.
		 * Use GetAreaColor to get an area color */
		private static Color[] AreaColors;
		
		/** Returns an color for an area, uses both user set ones and calculated.
		 * If the user has set a color for the area, it is used, but otherwise the color is calculated using Mathfx::IntToColor
		 * \see #AreaColors */
		public static Color GetAreaColor (int area) {
			if (AreaColors == null || area >= AreaColors.Length) {
				return Mathfx.IntToColor (area,1F);
			}
			return AreaColors[area];
		}
		
		/** Pushes all local variables out to static ones */
		public void OnEnable () {
			
			NodeConnection = _NodeConnection;
			UnwalkableNode = _UnwalkableNode;
			BoundsHandles = _BoundsHandles;
			
			ConnectionLowLerp = _ConnectionLowLerp;
			ConnectionHighLerp = _ConnectionHighLerp;
			
			MeshEdgeColor = _MeshEdgeColor;
			MeshColor = _MeshColor;
			
			AreaColors = _AreaColors;
		}
		
		public AstarColor () {
			
			_NodeConnection = new Color (0,1,0,0.5F);
			_UnwalkableNode = new Color (1,0,0,0.5F);
			_BoundsHandles = new Color (0.29F,0.454F,0.741F,0.9F);
			
			_ConnectionLowLerp = new Color (0,1,0,0.5F);
			_ConnectionHighLerp = new Color (1,0,0,0.5F);
			
			_MeshEdgeColor = new Color (0,0,0,0.5F);
			_MeshColor = new Color (0.125F, 0.686F, 0, 0.19F);
		}
		
		//new Color (0.909F,0.937F,0.243F,0.6F);
	}
	
	//Binary Heap
	
	/** Binary heap implementation. Binary heaps are really fast for ordering nodes in a way that makes it possible to get the node with the lowest F score. Also known as a priority queue.
	 * \see http://en.wikipedia.org/wiki/Binary_heap
	 */
	public class BinaryHeap { 
		public Node[] binaryHeap; 
		public int numberOfItems; 
	
		public BinaryHeap( int numberOfElements ) { 
			binaryHeap = new Node[numberOfElements]; 
			numberOfItems = 2;
		} 
		
		/** Adds a node to the heap */
		public void Add(Node node) {
			
			if (node == null) {
				Debug.Log ("Sending null node to BinaryHeap");
				return;
			}
			
			if (numberOfItems == binaryHeap.Length) {
				Debug.Log ("Forced to discard nodes because of binary heap size limit, please consider increasing the size ("+numberOfItems +" "+binaryHeap.Length+")");
				numberOfItems--;
			}
			
			binaryHeap[numberOfItems] = node;
			//node.heapIndex = numberOfItems;//Heap index
			
			int bubbleIndex = numberOfItems;
			int nodeF = node.f;
			
			while (bubbleIndex != 1) {
				int parentIndex = bubbleIndex / 2;
				
				/*if (binaryHeap[parentIndex] == null) {
					Debug.Log ("WUT!!");
					return;
				}*/
				
				if (nodeF <= binaryHeap[parentIndex].f) {
				   
					//binaryHeap[bubbleIndex].f <= binaryHeap[parentIndex].f) { /** \todo Wouldn't it be more efficient with '<' instead of '<=' ? * /
					//Node tmpValue = binaryHeap[parentIndex];
					
					//tmpValue.heapIndex = bubbleIndex;//HeapIndex
					
					binaryHeap[bubbleIndex] = binaryHeap[parentIndex];
					binaryHeap[parentIndex] = node;//binaryHeap[bubbleIndex];
					
					//binaryHeap[bubbleIndex].heapIndex = bubbleIndex; //Heap index
					//binaryHeap[parentIndex].heapIndex = parentIndex; //Heap index
					
					bubbleIndex = parentIndex;
				} else {
				/*if (binaryHeap[bubbleIndex].f <= binaryHeap[parentIndex].f) { /** \todo Wouldn't it be more efficient with '<' instead of '<=' ? *
					Node tmpValue = binaryHeap[parentIndex];
					
					//tmpValue.heapIndex = bubbleIndex;//HeapIndex
					
					
					binaryHeap[parentIndex] = binaryHeap[bubbleIndex];
					binaryHeap[bubbleIndex] = tmpValue;
					
					bubbleIndex = parentIndex;
				} else {*/
					break;
				}
			}
								 
			numberOfItems++;
		}
		
		/** Returns the node with the lowest F score from the heap */
		public Node Remove() {
			numberOfItems--;
			Node returnItem = binaryHeap[1];
			
		 	//returnItem.heapIndex = 0;//Heap index
			
			binaryHeap[1] = binaryHeap[numberOfItems];
			//binaryHeap[1].heapIndex = 1;//Heap index
			
			int swapItem = 1, parent = 1;
			
			do {
				parent = swapItem;
				int p2 = parent * 2;
				if ((p2 + 1) <= numberOfItems) {
					// Both children exist
					if (binaryHeap[parent].f >= binaryHeap[p2].f) {
						swapItem = p2;//2 * parent;
					}
					if (binaryHeap[swapItem].f >= binaryHeap[p2 + 1].f) {
						swapItem = p2 + 1;
					}
				} else if ((p2) <= numberOfItems) {
					// Only one child exists
					if (binaryHeap[parent].f >= binaryHeap[p2].f) {
						swapItem = p2;
					}
				}
				
				// One if the parent's children are smaller or equal, swap them
				if (parent != swapItem) {
					Node tmpIndex = binaryHeap[parent];
					//tmpIndex.heapIndex = swapItem;//Heap index
					
					binaryHeap[parent] = binaryHeap[swapItem];
					binaryHeap[swapItem] = tmpIndex;
					
					//binaryHeap[parent].heapIndex = parent;//Heap index
				}
			} while (parent != swapItem);
			
			return returnItem;
		}
		
		/** \deprecated Use #Add instead */
		public void BubbleDown (Node node) {
			
			//int bubbleIndex = node.heapIndex;
			int bubbleIndex = 0;
			
			if (bubbleIndex < 1 || bubbleIndex > numberOfItems) {
				Debug.LogError ("Node is not in the heap (index "+bubbleIndex+")");
				Add (node);
				return;
			}
			
			while (bubbleIndex != 1) {
				int parentIndex = bubbleIndex / 2;
				
				/* Can be optimized to use 'node' instead of 'binaryHeap[bubbleIndex]' */
				if (binaryHeap[bubbleIndex].f <= binaryHeap[parentIndex].f) {
					Node tmpValue = binaryHeap[parentIndex];
					binaryHeap[parentIndex] = binaryHeap[bubbleIndex];
					binaryHeap[bubbleIndex] = tmpValue;
					
					//binaryHeap[parentIndex].heapIndex = parentIndex;
					//binaryHeap[bubbleIndex].heapIndex = bubbleIndex;
					
					bubbleIndex = parentIndex;
				} else {
					return;
				}
			}
		}
		
		/** Rebuilds the heap by trickeling down all items. Called after the hTarget on a path has been changed */
		public void Rebuild () {
			
			for (int i=2;i<numberOfItems;i++) {
				int bubbleIndex = i;
				Node node = binaryHeap[i];
				int nodeF = node.f;
				while (bubbleIndex != 1) {
					int parentIndex = bubbleIndex / 2;
					
					if (nodeF < binaryHeap[parentIndex].f) {
						//Node tmpValue = binaryHeap[parentIndex];
						binaryHeap[bubbleIndex] = binaryHeap[parentIndex];
						binaryHeap[parentIndex] = node;
						bubbleIndex = parentIndex;
					} else {
						break;
					}
				}
				
			}
			
			
		}
		
		/** Rearranges a node in the heap which has got it's F score changed (only works for a lower F score). \warning This is slow, often it is more efficient to just add the node to the heap again */
		public void Rearrange (Node node) {
			
			for (int i=0;i<numberOfItems;i++) {
				if (binaryHeap[i] == node) {
					
					int bubbleIndex = i;
					while (bubbleIndex != 1) {
						int parentIndex = bubbleIndex / 2;
						
						if (binaryHeap[bubbleIndex].f <= binaryHeap[parentIndex].f) {
							Node tmpValue = binaryHeap[parentIndex];
							binaryHeap[parentIndex] = binaryHeap[bubbleIndex];
							binaryHeap[bubbleIndex] = tmpValue;
							bubbleIndex = parentIndex;
						} else {
							return;
						}
					}
				}
			}
		}
		
		/** Returns a nicely formatted string describing the tree structure. '!!!' marks after a value means that the tree is not correct at that node (i.e it should be swapped with it's parent) */
		public override string ToString () {
			System.Text.StringBuilder text = new System.Text.StringBuilder ();
			
			text.Append ("\n=== Writing Binary Heap ===\n");
			text.Append ("Number of items: ").Append (numberOfItems-2);
			text.Append ("Capacity: ").Append (binaryHeap.Length);
			text.Append ("\n");
			if (numberOfItems > 2) {
				WriteBranch (1,1,text);
			}
			text.Append ("\n\n");
			return text.ToString ();
		}
		
		/** Writes a branch of the tree to a StringBuilder. Used by #ToString */
		private void WriteBranch (int index, int depth, System.Text.StringBuilder text) {
			text.Append ("\n");
			for (int i=0;i<depth;i++) {
				text.Append ("   ");
			}
			
			text.Append (binaryHeap[index].f);
			
			if (index > 1) {
				int parentIndex = index / 2;
						
				if (binaryHeap[index].f < binaryHeap[parentIndex].f) {
					text.Append ("	!!!");
				}
			}
			
			int p2 = index * 2;
			if ((p2 + 1) <= numberOfItems) {
				// Both children exist
				WriteBranch (p2,depth+1,text);
				WriteBranch (p2+1,depth+1,text);
			} else if (p2 <= numberOfItems) {
				// Only one child exists
				WriteBranch (p2,depth+1,text);
			}
		}
		
	}
	
	/** Returned by graph ray- or linecasts containing info about the hit. This will only be set up if something was hit. \todo Why isn't this a struct? */
	public class GraphHitInfo {
		public Vector3 origin;
		public Vector3 point;
		public Node node;
		public Vector3 tangentOrigin;
		public Vector3 tangent;
		public bool success;
		
		public float distance {
			get {
				return (point-origin).magnitude;
			}
		}
		
		public GraphHitInfo () {
			success = false;
			tangentOrigin  = Vector3.zero;
			origin = Vector3.zero;
			point = Vector3.zero;
			node = null;
			tangent = Vector3.zero;
		}
		
		public GraphHitInfo (Vector3 point) {
			success = false;
			tangentOrigin  = Vector3.zero;
			origin = Vector3.zero;
			this.point = point;
			node = null;
			tangent = Vector3.zero;
			//this.distance = distance;
		}
	}
	
	/** Nearest node constraint. Constrains which nodes will be returned by the GetNearest function */
	public class NNConstraint {
		
		
		public bool constrainArea = false; /**< Only treat nodes in the area #area as suitable. Does not affect anything if #area is less than 0 (zero) */ 
		public int area = -1; /**< Area ID to constrain to. Will not affect anything if less than 0 (zero) or if #constrainArea is false */
		
		public bool constrainWalkability = true; /**< Only treat nodes with the walkable flag set to the same as #walkable as suitable */
		public bool walkable = true; /**< What must the walkable flag on a node be for it to be suitable. Does not affect anything if #constrainWalkability if false */
		
		
		/** Returns whether or not the node conforms to this NNConstraint's rules */
		public virtual bool Suitable (Node node) {
			if (constrainWalkability && node.walkable != walkable) return false;
			
			if (constrainArea && area >= 0 && node.area != area) return false;
			
			return true;
		}
		
		/** The default NNConstraint */
		public static NNConstraint Default {
			get {
				return new NNConstraint ();
			}
		}
		
		/** Returns a constraint which will not filter the results */
		public static NNConstraint None {
			get {
				NNConstraint n = new NNConstraint ();
				n.constrainWalkability = false;
				n.constrainArea = false;
				return n;
			}
		}
		
		/** Default constructor. Equals to the property #Default */
		public NNConstraint () {
		}
	}
	
	/** A special NNConstraint which can use different logic for the start node and end node in a path.
	 * A PathNNConstraint can be assigned to the Path::nnConstraint field, the path will first search for the start node, then it will call #SetStart and proceed with searching for the end node (nodes in the case of a MultiTargetPath).\n
	 * The #Default PathNNConstraint will constrain the end point to lie inside the same area as the start point.
	 */
	public class PathNNConstraint : NNConstraint {
		
		public static new PathNNConstraint Default {
			get {
				PathNNConstraint n = new PathNNConstraint ();
				n.constrainArea = true;
				return n;
			}
		}
		
		/** Called after the start node has been found. This is used to get different search logic for the start and end nodes in a path */
		public virtual void SetStart (Node node) {
			if (node != null) {
				area = node.area;
			} else {
				constrainArea = false;
			}
		}
	}
	
	public struct NNInfo {
		public Node node;
		
		/** Optional to be filled in. if the search will be able to find the constrained node without any extra effort it can fill it in. */
		public Node constrainedNode;
		
		public NearestNodePriority priority;
		public Vector3 clampedPosition;
		/** Clamped position for the optional constrainedNode */
		public Vector3 constClampedPosition;
		
		public NNInfo (Node node) {
			priority = NearestNodePriority.Normal;
			this.node = node;
			constrainedNode = null;
			constClampedPosition = Vector3.zero;
			
			if (node != null) {
				clampedPosition = node.position;
			} else {
				clampedPosition = Vector3.zero;
			}
		}
		
		/** Sets the constrained node */
		public void SetConstrained (Node constrainedNode, Vector3 clampedPosition) {
			this.constrainedNode = constrainedNode;
			constClampedPosition = clampedPosition;
		}
		
		/** Updates #clampedPosition and #constClampedPosition from node positions */
		public void UpdateInfo () {
			if (node != null) {
				clampedPosition = node.position;
			} else {
				clampedPosition = Vector3.zero;
			}
			
			if (constrainedNode != null) {
				constClampedPosition = constrainedNode.position;
			} else {
				constClampedPosition = Vector3.zero;
			}
		}
		
		public NNInfo (Node node, NearestNodePriority priority) {
			this.node = node;
			this.priority = priority;
			clampedPosition = node.position;
			constrainedNode = null;
			constClampedPosition = Vector3.zero;
		}
		
		public static implicit operator Vector3 (NNInfo ob) {
			return ob.clampedPosition;
		}
		
		public static implicit operator Node (NNInfo ob) {
			return ob.node;
		}
		
		public static implicit operator NNInfo (Node ob) {
			return new NNInfo (ob);
		}
	}
	
	public struct Progress {
		public float progress;
		public string description;
		
		public Progress (float p, string d) {
			progress = p;
			description = d;
		}
	}
	
	public interface IUpdatableGraph {
		
		/** Updates an area using the specified GraphUpdateObject.
		 * 
		 * Notes to implementators.
		 * This function should (in order):
		 * -# Call o.WillUpdateNode on the GUO for every node it will update, it is important that this is called BEFORE any changes are made to the nodes.
		 * -# Update walkabilty using special settings such as the usePhysics flag used with the GridGraph.
		 * -# Call Apply on the GUO for every node which should be updated with the GUO.
		 * -# Update eventual connectivity info if appropriate (GridGraphs updates connectivity, but most other graphs don't since then the connectivity cannot be recovered later).
		 */
		void UpdateArea (GraphUpdateObject o);
	}
	
	//Enumerators
	
	/** Used to weight nodes returned by the GetNearest function from different graphs */
	public enum NearestNodePriority {
		ReallyLow = 20, 	
		Low = 8,		/**< Used when only a simple range check or a similar algorithm was used */
		Normal = 5, 	/**< Default */
		High = 1,		/**< Used when the node is a very good candidate to being the closest node (like when the position is inside a triangle on a navmesh), it is really hard to override this */
		ReallyHigh = 0 	/**< No other NavGraph can override this except if another graph returned ReallyHigh before this one */
	}
	
	/** Represents a collection of settings used to update nodes in a specific area of a graph.
	 * \see AstarPath::UpdateGraphs
	 */
	public class GraphUpdateObject {
		
		/** The bounds to update nodes within */
		public Bounds bounds;
		
		/** Performance boost.
		 * This controlls if a flood fill will be carried out after this GUO has been applied.\n
		 * If you are sure that a GUO will not modify walkability or connections. You can set this to false.
		 * For example when only updating penalty values it can save processing power when setting this to false. Especially on large graphs.
		 * \note If you set this to false, even though it does change e.g walkability, it can lead to paths returning that they failed even though there is a path,
		 * or the try to search the whole graph for a path even though there is none, and will in the processes use wast amounts of processing power.
		 *
		 * If using the basic GraphUpdateObject (not a derived class), a quick way to check if it is going to need a flood fill is to check if #modifyWalkability is true or #updatePhysics is true.
		 *
		 */
		public bool requiresFloodFill = true;
		
		/** Use physics checks to update nodes.
		 * When updating a grid graph and this is true, the nodes' position and walkability will be updated using physics checks
		 * with settings from "Collision Testing" and "Height Testing".
		 * Also when updating a ListGraph, setting this to true will make it re-evaluate all connections in the graph which passes through the #bounds.
		 * This has no effect when updating GridGraphs if #modifyWalkability is turned on */
		public bool updatePhysics = true;
		
		/** NNConstraint to use.
		 * The Pathfinding::NNConstraint::SuitableGraph function will be called on the NNConstraint to enable filtering of which graphs to update.\n
		 * \note As the Pathfinding::NNConstraint::SuitableGraph function is A* Pathfinding Project Pro only, this variable doesn't really affect anything in the free version.
		 * 
		 * \astarpro */
		public NNConstraint nnConstraint = NNConstraint.None;
		
		/** Penalty to add to the nodes */
		public int addPenalty = 0;
		
		public bool modifyWalkability = false; /**< If true, all nodes \a walkable variables will be set to #setWalkability */
		public bool setWalkability = false; /**< If #modifyWalkability is true, the nodes' \a walkable variable will be set to this */
		
		public int tagsChange = 0; 	/**< Which tags to change */
		public int tagsValue = 0;	/**< The values to which the tags will be changed */
		
		public bool trackChangedNodes = false;
		public List<Node> changedNodes;
		public List<ulong> backupData;
		
		/** Should be called on every node which is updated with this GUO before it is updated */
		public virtual void WillUpdateNode (Node node) {
			if (trackChangedNodes) {
				if (changedNodes == null) { changedNodes = new List<Node>(); backupData = new List<ulong>(); }
				changedNodes.Add (node);
				backupData.Add ((ulong)node.penalty<<32 | (ulong)node.flags);
			}
		}
		
		/** Reverts penalties and flags (which includes walkability) on every node which was updated using this GUO.
		 * Data for reversion is only saved if #trackChangedNodes is true */
		public virtual void RevertFromBackup () {
			if (trackChangedNodes) {
				for (int i=0;i<changedNodes.Count;i++) {
					changedNodes[i].penalty = (int)(backupData[i]>>32);
					changedNodes[i].flags = (int)(backupData[i] & 0xFFFFFFFF);
				}
			} else {
				Debug.LogWarning ("Changed nodes have not been tracked, cannot revert from backup");
			}
		}
		
		/** Updates the specified node using this GUO's settings */
		public virtual void Apply (Node node) {
			node.penalty += addPenalty;
			if (modifyWalkability) {
				node.walkable = setWalkability;
			}
			
			node.tags = (node.tags & ~tagsChange) | (tagsValue & tagsChange);
			
		}
		
		public GraphUpdateObject () {
		}
		
		/** Creates a new GUO with the specified bounds */
		public GraphUpdateObject (Bounds b) {
			bounds = b;
		}
	}
	
	public interface IRaycastableGraph {
		bool Linecast (Vector3 start, Vector3 end);
		bool Linecast (Vector3 start, Vector3 end, Node hint);
		bool Linecast (Vector3 start, Vector3 end, Node hint, out GraphHitInfo hit);
	}
}

//Delegates
	
//public delegate (Path p

/* Delegate with on Path object as parameter.
 * This is used for callbacks when a path has finished calculation.\n
 * Example function:
 * \code
public void Start () {
	//Assumes a Seeker component is attached to the GameObject
	Seeker seeker = GetComponent<Seeker>();
	
	//seeker.pathCallback is a OnPathDelegate, we add the function OnPathComplete to it so it will be called whenever a path has finished calculating on that seeker
	seeker.pathCallback += OnPathComplete;
}

public void OnPathComplete (Path p) {
	Debug.Log ("This is called when a path is completed on the seeker attached to this GameObject");
}\endcode
  */
public delegate void OnPathDelegate (Path p);

public delegate Vector3[] GetNextTargetDelegate (Path p, Vector3 currentPosition);

//public delegate void OnPathSucess (Path p);

//public delegate void OnPathError (Path p);

//public delegate void OnPathPreSearch (Path p);

//public delegate void OnPathPostSearch (Path p);

public delegate void OnGraphDelegate (NavGraph graph);

//public delegate void OnGraphPostScan (NavGraph graph);

public delegate void OnScanDelegate (AstarPath script);

public delegate void OnVoidDelegate ();

//public delegate void OnPostScan ();

/** How path results are logged by the system */
public enum PathLog {
	None,		/**< Does not log anything */
	Normal,		/**< Logs basic info about the paths */
	Heavy,		/**< Includes additional info */
	InGame,		/**< Same as heavy, but displays the info in-game using GUI */
	OnlyErrors	/**< Same as normal, but logs only paths which returned an error */
}

/** Heuristic to use. Heuristic is the estimated cost from the current node to the target */
public enum Heuristic {
	Manhattan,
	DiagonalManhattan,
	Euclidean,
	None
}

/** What data to draw the graph debugging with */
public enum GraphDebugMode {
	Areas,
	G,
	H,
	F,
	Penalty,
	Connections
}

/** Type of connection for a user placed link */
public enum ConnectionType {
	Connection,
	ModifyNode
}