using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using Pathfinding;
	
namespace Pathfinding {
	[System.Serializable]
	/** Stores the navgraphs for the A* Pathfinding System.
	 * \ingroup relevant
	 * 
	 * An instance of this class is assigned to AstarPath::astarData, from it you can access all graphs loaded through #graphs.\n
	 * This class also handles a lot of the high level serialization. */
	public class AstarData {
	
		public AstarPath active {
			get {
				return AstarPath.active;
			}
		}
		
		[System.NonSerialized]
		public NavMeshGraph navmesh; 	/**< Shortcut to the first NavMeshGraph. Updated at scanning time */
		
		[System.NonSerialized]
		public GridGraph gridGraph;		/**< Shortcut to the first GridGraph. Updated at scanning time */
		
		[System.NonSerialized]
		public ListGraph listGraph;		/**< Shortcut to the first ListGraph. Updated at scanning time */
		
		public void UpdateShortcuts () {
			navmesh = (NavMeshGraph)FindGraphOfType (typeof(NavMeshGraph));
			gridGraph = (GridGraph)FindGraphOfType (typeof(GridGraph));
			listGraph = (ListGraph)FindGraphOfType (typeof(ListGraph));
		}
		
		/** All supported graph types. This should be identical throughout the project */
		public System.Type[] graphTypes = null;
		//public string[] graphTypeIdentifiers = null; //This array needs to have the same length as the previous
		//new NavGraph[5] {GridGraph.NewInstance (),NavMeshGraph.NewInstance (),MultiGridGraph.NewInstance (),LineTraceGraph.NewInstance (),RecastGraph.NewInstance ()};
			
		/*public GridGraph[] graphs_grid;
		public MultiGridGraph[] graphs_multigrid;
		public NavMeshGraph[] graphs_navmesh;
		public LineTraceGraph[] graphs_lineTrace;
		public RecastGraph[] graphs_recast;
		public CustomGridGraph[] graphs_custom_grid;*/
		
		[System.NonSerialized]
		/** All graphs this instance holds. This will be filled only after deserialization has completed */
		public NavGraph[] graphs;
		
		/** Links placed by the user in the scene view. */
		[System.NonSerialized]
		public UserConnection[] userConnections;
		
		//Serialization Settings
		
		/** Has the data been reverted by an undo operation. Used by the editor's undo logic to check if the AstarData has been reverted by an undo operation and should be deserialized */
		public bool hasBeenReverted = false;
		
		[SerializeField]
		public byte[] data;
		
		public byte[] data_backup;
		
		public byte[] data_cachedStartup;
		
		public bool cacheStartup = false;
		
		public bool compress = false;
		
		//End Serialization Settings
		
		/** Loads the graphs from memory, will load cached graphs if any exists */
		public void Awake () {
			
			
			if (cacheStartup && data_cachedStartup != null) {
				LoadFromCache ();
			} else {
				AstarSerializer serializer = new AstarSerializer (active);
				DeserializeGraphs (serializer);
			}
			
			
		}
		
		public void LoadFromCache () {
			if (data_cachedStartup != null && data_cachedStartup.Length > 0) {
				AstarSerializer serializer = new AstarSerializer (active);
				DeserializeGraphs (serializer,data_cachedStartup);
			} else {
				Debug.LogError ("Can't load from cache since the cache is empty");
			}
		}
		
		public void SaveCacheData (int mask) {
			
			AstarSerializer serializer = new AstarSerializer (active);
			serializer.mask = mask;//AstarSerializer.SaveNodes;
			data_cachedStartup = SerializeGraphs (serializer);
			cacheStartup = true;
			
		}
		
		/** Main serializer function, a similar one exists in the AstarEditor.cs script to save additional info */
		public byte[] SerializeGraphs (AstarSerializer serializer) {
			
			serializer.OpenSerialize ();
			
			SerializeGraphsPart (serializer);
			
			serializer.Close ();
			
			byte[] bytes = (serializer.writerStream.BaseStream as System.IO.MemoryStream).ToArray ();
			
			Debug.Log ("Got a whole bunch of data, "+bytes.Length+" bytes");
			return bytes;
		}
		
		/** Saves all graphs and also user connections, but does not close, nor opens the stream */
		public void SerializeGraphsPart (AstarSerializer serializer) {
			
			//AstarSerializer serializer = new AstarSerializer ();
			
			//serializer.OpenSerializeSettings ();
			SizeProfiler.Initialize ();
			SizeProfiler.Begin ("File",serializer.writerStream,false);
			SizeProfiler.Begin ("Graphs init",serializer.writerStream);
			serializer.writerStream.Write (graphs.Length);
			serializer.writerStream.Write (graphs.Length);
			
			SizeProfiler.End ("Graphs init",serializer.writerStream);
			
			int[] masks = new int[graphs.Length];
			
			for (int i=0;i<graphs.Length;i++) {
				NavGraph graph = graphs[i];
				
				int tmpMask = serializer.mask;
				
				SizeProfiler.Begin ("Graphs type "+i,serializer.writerStream);
				
				serializer.AddAnchor ("Graph"+i);
	        	serializer.writerStream.Write (graph.GetType ().Name);
	        	serializer.writerStream.Write (graph.guid.ToString ());
	        	
	        	SizeProfiler.Begin ("Graphs settings "+i,serializer.writerStream);
	        	
				//Set an unique prefix for all variables in this graph
				serializer.sPrefix = i.ToString ();
				serializer.SerializeSettings (graph,active);
				serializer.sPrefix = "";
				
				masks[i] = serializer.mask;
				serializer.mask = tmpMask;
				
				SizeProfiler.End ("Graphs settings "+i,serializer.writerStream);
			}
			

			//Serialize nodes
			for (int i=0;i<graphs.Length;i++) {
				NavGraph graph = graphs[i];
				
				serializer.mask = masks[i];
				
				SizeProfiler.Begin ("Graphs nodes "+i,serializer.writerStream,false);
				
				serializer.AddAnchor ("GraphNodes_Graph"+i);
				
				serializer.writerStream.Write (masks[i]);
				serializer.sPrefix = i.ToString ()+"N";
				serializer.SerializeNodes (graph,active);
				serializer.sPrefix = "";
				SizeProfiler.End ("Graphs nodes "+i,serializer.writerStream);
			}
			
			SizeProfiler.Begin ("User Connections",serializer.writerStream);
			
			serializer.SerializeUserConnections (userConnections);
			
			SizeProfiler.End ("User Connections",serializer.writerStream);
			//data = (serializer.writerStream.BaseStream as System.IO.MemoryStream).ToArray ();
			//serializer.Close ();
			SizeProfiler.End ("File",serializer.writerStream);
			SizeProfiler.Log ();
		}
		
		/** Main deserializer function, loads from the #data variable */
		public void DeserializeGraphs (AstarSerializer serializer) {
			DeserializeGraphs (serializer, data);
		}
		
		/** Main deserializer function, loads from \a bytes variable */
		public void DeserializeGraphs (AstarSerializer serializer, byte[] bytes) {
			
			System.DateTime startTime = System.DateTime.UtcNow;
			
			if (bytes == null || bytes.Length == 0) {
				Debug.Log ("No previous data, assigning default");
				graphs = new NavGraph[0];
				return;
			}
			
			Debug.Log ("Deserializing...");
			
			serializer = serializer.OpenDeserialize (bytes);
			
			DeserializeGraphsPart (serializer);
			
			serializer.Close ();
			
			System.DateTime endTime = System.DateTime.UtcNow;
			Debug.Log ("Deserialization complete - Process took "+((endTime-startTime).Ticks*0.0001F).ToString ("0.00")+" ms");
		}
		
		/** Deserializes all graphs and also user connections */
		public void DeserializeGraphsPart (AstarSerializer serializer) {
			
			if (serializer.error != AstarSerializer.SerializerError.Nothing) {
				data_backup = (serializer.readerStream.BaseStream as System.IO.MemoryStream).ToArray ();
				Debug.Log ("Error encountered : "+serializer.error+"\nWriting data to AstarData.data_backup");
				graphs = new NavGraph[0];
				return;
			}
			
			try {
				int count1 = serializer.readerStream.ReadInt32 ();
				int count2 = serializer.readerStream.ReadInt32 ();
				
				if (count1 != count2) {
					Debug.LogError ("Data is corrupt ("+count1 +" != "+count2+")");
					graphs = new NavGraph[0];
					return;
				}
				
				NavGraph[] _graphs = new NavGraph[count1];
				//graphs = new NavGraph[count1];
				
				for (int i=0;i<_graphs.Length;i++) {
					
					if (!serializer.MoveToAnchor ("Graph"+i)) {
						Debug.LogError ("Couldn't find graph "+i+" in the data");
						Debug.Log ("Logging... "+serializer.anchors.Count);
						foreach (KeyValuePair<string,int> value in serializer.anchors) {
							Debug.Log ("KeyValuePair "+value.Key);
						}
						_graphs[i] = null;
						continue;
					}
					string graphType = serializer.readerStream.ReadString ();
					
					System.Guid guid = new System.Guid (serializer.readerStream.ReadString ());
					
					//Search for existing graphs with the same GUID. If one is found, that means that we are loading another version of that graph
					//Use that graph then and just load it with some new settings
					NavGraph existingGraph = GuidToGraph (guid);
					
					if (existingGraph != null) {
						_graphs[i] = existingGraph;
						//Replace
						//graph.guid = new System.Guid ();
						//serializer.loadedGraphGuids[i] = graph.guid.ToString ();
					} else {
						_graphs[i] = CreateGraph (graphType);
					}
					
					NavGraph graph = _graphs[i];
					
					if (graph == null) {
						Debug.LogError ("One of the graphs saved was of an unknown type, the graph was of type '"+graphType+"'");
						data_backup = data;
						graphs = new NavGraph[0];
						return;
					}
					
					_graphs[i].guid = guid;
					
					//Set an unique prefix for all variables in this graph
					serializer.sPrefix = i.ToString ();
					serializer.DeSerializeSettings (graph,active);
				}
				
				serializer.SetUpGraphRefs (_graphs);
				
	
				for (int i=0;i<_graphs.Length;i++) {
					
					NavGraph graph = _graphs[i];
					
					if (serializer.MoveToAnchor ("GraphNodes_Graph"+i)) {
						serializer.mask = serializer.readerStream.ReadInt32 ();
						serializer.sPrefix = i.ToString ()+"N";
						serializer.DeserializeNodes (graph,_graphs,i,active);
						serializer.sPrefix = "";
					}
					
					//Debug.Log ("Graph "+i+" has loaded "+(graph.nodes != null ? graph.nodes.Length.ToString () : "null")+" nodes");
					
				}
				
				userConnections = serializer.DeserializeUserConnections ();
				
				//Remove null graphs
				List<NavGraph> tmp = new List<NavGraph>(_graphs);
				for (int i=0;i<_graphs.Length;i++) {
					if (_graphs[i] == null) {
						tmp.Remove (_graphs[i]);
					}
				}
				
				graphs = tmp.ToArray ();
			} catch (System.Exception e) {
				data_backup = (serializer.readerStream.BaseStream as System.IO.MemoryStream).ToArray ();
				Debug.LogWarning ("Deserializing Error Encountered - Writing data to AstarData.data_backup:\n"+e.ToString ());
				graphs = new NavGraph[0];
				return;
			}
		}
		
		//Collects all graphs from the Unity-serialized type-dependant arrays into one single array ('graphs', of type NavGraph[])
		/*public void CollectGraphs () {
			
				graphs_grid = 			graphs_grid			== null ? new GridGraph[0] 		: graphs_grid;
				graphs_custom_grid = 	graphs_custom_grid	== null ? new CustomGridGraph[0]: graphs_custom_grid;
				graphs_navmesh = 		graphs_navmesh		== null ? new NavMeshGraph[0] 	: graphs_navmesh;
				graphs_multigrid = 		graphs_multigrid	== null ? new MultiGridGraph[0] : graphs_multigrid;
				graphs_lineTrace = 		graphs_lineTrace	== null ? new LineTraceGraph[0] : graphs_lineTrace;
				graphs_recast = 		graphs_recast		== null ? new RecastGraph[0] 	: graphs_recast;
			
			List<NavGraph> gr = new List<NavGraph> (graphs_grid);
			gr.AddRange (graphs_navmesh);
			gr.AddRange (graphs_multigrid);
			gr.AddRange (graphs_lineTrace);
			gr.AddRange (graphs_recast);
			gr.AddRange (graphs_custom_grid);
			graphs = gr.ToArray ();
			
			if (graphs.Length > 32) {
				Debug.LogError ("Error, the system doesn't support more than 32 graphs active at once, expect errors");
			}
		}*/
		
		/** Find all graph types supported in this build. Using reflection, the assembly is searched for types which inherit from NavGraph. */
		public void FindGraphTypes () {
			
			System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly (typeof(AstarPath));
			
			System.Type[] types = asm.GetTypes ();
			
			List<System.Type> graphList = new List<System.Type> ();
			
			foreach (System.Type type in types) {
				
				System.Type baseType = type.BaseType;
				while (baseType != null) {
					
					if (baseType == typeof(NavGraph)) {
						
						graphList.Add (type);
						
						break;
					}
					
					baseType = baseType.BaseType;
				}
			}
			
			graphTypes = graphList.ToArray ();
			
			Debug.Log ("Found "+graphTypes.Length+" graph types");
			
		}
		
		/** \returns A System.Type which matches the specified \a type string. If no mathing graph type was found, null is returned */
		public System.Type GetGraphType (string type) {
			for (int i=0;i<graphTypes.Length;i++) {
				
				if (graphTypes[i].Name == type) {
					return graphTypes[i];
				}
			}
			return null;
		}
		
		/** Creates a new instance of a graph of type \a type. If no matching graph type was found, an error is logged and null is returned
		 * \returns The created graph 
		 * \see CreateGraph(System.Type) */
		public NavGraph CreateGraph (string type) {
			Debug.Log ("Creating Graph of type '"+type+"'");
			
			for (int i=0;i<graphTypes.Length;i++) {
				
				if (graphTypes[i].Name == type) {
					return CreateGraph (graphTypes[i]);
				}
			}
			Debug.LogError ("Graph type ("+type+") wasn't found");
			return null;
		}
		
		/** Creates a new graph instance of type \a type
		 * \see CreateGraph(string) */
		public NavGraph CreateGraph (System.Type type) {
			NavGraph g = System.Activator.CreateInstance (type) as NavGraph;
			g.active = active;
			return g;
		}
		
		/** Adds a graph of type \a type to the #graphs array */
		public NavGraph AddGraph (string type) {
			NavGraph graph = null;
			
			for (int i=0;i<graphTypes.Length;i++) {
				
				if (graphTypes[i].Name == type) {
					graph = CreateGraph (graphTypes[i]);
				}
			}
			
			if (graph == null) {
				Debug.LogError ("No NavGraph of type '"+type+"' could be found");
				return null;
			}
			
			AddGraph (graph);
			
			return graph;
		}
		
		/** Adds a graph of type \a type to the #graphs array */
		public NavGraph AddGraph (System.Type type) {
			NavGraph graph = null;
			
			for (int i=0;i<graphTypes.Length;i++) {
				
				if (graphTypes[i] == type) {
					graph = CreateGraph (graphTypes[i]);
				}
			}
			
			if (graph == null) {
				Debug.LogError ("No NavGraph of type '"+type+"' could be found, "+graphTypes.Length+" graph types are avaliable");
				return null;
			}
			
			AddGraph (graph);
			
			return graph;
		}
		
		/** Adds the specified graph to the #graphs array */
		public void AddGraph (NavGraph graph) {
			
			List<NavGraph> ls = new List<NavGraph> (graphs);
			ls.Add (graph);
			graphs = ls.ToArray ();
		}
		
		/** Removes the specified graph from the #graphs array and Destroys it in a safe manner */
		public void RemoveGraph (NavGraph graph) {
			
			List<NavGraph> ls = new List<NavGraph> (graphs);
			ls.Remove (graph);
			graphs = ls.ToArray ();
			
			//Safe OnDestroy is called since there is a risk that the pathfinding is searching through the graph right now, and if we don't wait until the search has completed we could end up with evil NullReferenceExceptions
			graph.SafeOnDestroy ();
		}
		
		/** Returns the graph which contains the specified node. The graph must be in the #graphs array.
		 * \returns Returns the graph which contains the node. Null if the graph wasn't found */
		public static NavGraph GetGraph (Node node) {
			
			if (node == null) {
				return null;
			}
			
			AstarPath script = AstarPath.active;
			
			if (script == null) return null;
			
			AstarData data = script.astarData;
			
			if (data == null) return null;
			
			if (data.graphs == null) return null;
			
			int graphIndex = node.graphIndex;
			
			if (graphIndex < 0 || graphIndex >= data.graphs.Length) {
				return null;
			}
			
			return data.graphs[graphIndex];
		}
		
		/** Returns the node at \a graphs[graphIndex].nodes[nodeIndex]. All kinds of error checking is done to make sure no exceptions are thrown. */
		public Node GetNode (int graphIndex, int nodeIndex) {
			return GetNode (graphIndex,nodeIndex, graphs);
		}
		
		/** Returns the node at \a graphs[graphIndex].nodes[nodeIndex]. The graphIndex refers to the specified graphs array.\n
		 * All kinds of error checking is done to make sure no exceptions are thrown */
		public Node GetNode (int graphIndex, int nodeIndex, NavGraph[] graphs) {
			
			if (graphs == null) {
				return null;
			}
			
			if (graphIndex < 0 || graphIndex >= graphs.Length) {
				Debug.LogError ("Graph index is out of range"+graphIndex+ " [0-"+(graphs.Length-1)+"]");
				return null;
			}
			
			NavGraph graph = graphs[graphIndex];
			
			if (graph.nodes == null) {
				return null;
			}
			
			if (nodeIndex < 0 || nodeIndex >= graph.nodes.Length) {
				Debug.LogError ("Node index is out of range : "+nodeIndex+ " [0-"+(graph.nodes.Length-1)+"]");
				return null;
			}
			
			return graph.nodes[nodeIndex];
		}
		
		/** Returns the first graph of type \a type found in the #graphs array. Returns null if none was found */
		public NavGraph FindGraphOfType (System.Type type) {
			
			for (int i=0;i<graphs.Length;i++) {
				if (graphs[i].GetType () == type) {
					return graphs[i];
				}
			}
			return null;
		}
		
		/** Loop through this function to get all graphs of type 'type' 
		 * \code foreach (GridGraph graph in AstarPath.astarData.FindGraphsOfType (typeof(GridGraph))) {
		 * 	//Do something with the graph
		 * } \endcode
		 * \see AstarPath::RegisterSafeNodeUpdate */
		public IEnumerable FindGraphsOfType (System.Type type) {
			if (graphs == null) { yield break; }
			for (int i=0;i<graphs.Length;i++) {
				if (graphs[i].GetType () == type) {
					yield return graphs[i];
				}
			}
		}
		
		/** All graphs which implements the UpdateableGraph interface
		 * \code foreach (IUpdatableGraph graph in AstarPath.astarData.GetUpdateableGraphs ()) {
		 * 	//Do something with the graph
		 * } \endcode
		 * \see AstarPath::RegisterSafeNodeUpdate
		 * \see Pathfinding::IUpdatableGraph */
		public IEnumerable GetUpdateableGraphs () {
			if (graphs == null) { yield break; }
			for (int i=0;i<graphs.Length;i++) {
				if (graphs[i] is IUpdatableGraph) {
					yield return graphs[i];
				}
			}
		}
		
		/** All graphs which implements the UpdateableGraph interface
		  * \code foreach (IRaycastableGraph graph in AstarPath.astarData.GetRaycastableGraphs ()) {
		 * 	//Do something with the graph
		 * } \endcode
		 * \see Pathfinding::IRaycastableGraph*/
		public IEnumerable GetRaycastableGraphs () {
			if (graphs == null) { yield break; }
			for (int i=0;i<graphs.Length;i++) {
				if (graphs[i] is IRaycastableGraph) {
					yield return graphs[i];
				}
			}
		}
		
		/** Gets the index of the NavGraph in the #graphs array */
		public int GetGraphIndex (NavGraph graph) {
			for (int i=0;i<graphs.Length;i++) {
				if (graph == graphs[i]) {
					return i;
				}
			}
			Debug.LogError ("Graph doesn't exist");
			return -1;
		}
	
		/** Tries to find a graph with the specified GUID in the #graphs array. if one is found it returns its index, otherwise it returns -1
		 * \see GuidToGraph */
		public int GuidToIndex (System.Guid guid) {
			
			if (graphs == null) {
				return -1;
				//CollectGraphs ();
			}
			
			for (int i=0;i<graphs.Length;i++) {
				if (graphs[i] == null) {
					Debug.LogWarning ("Graph "+i+" is null - This should not happen");
					continue;
				}
				if (graphs[i].guid == guid) {
					return i;
				}
			}
			return -1;
		}
		
		/** Tries to find a graph with the specified GUID in the #graphs array. Returns null if none is found
		 * \see GuidToIndex */
		public NavGraph GuidToGraph (System.Guid guid) {
			
			if (graphs == null) {
				return null;
				//CollectGraphs ();
			}
			
			for (int i=0;i<graphs.Length;i++) {
				if (graphs[i] == null) {
					Debug.LogWarning ("Graph "+i+" is null - This should not happen");
					continue;
				}
				if (graphs[i].guid == guid) {
					return graphs[i];
				}
			}
			return null;
		}
	}
}