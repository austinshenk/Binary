//Define optimizations is a A* Pathfinding Project Pro only feature
//#define ProfileAstar	//Enables profiling of the pathfinding process in multithreaded mode
//#define DEBUG			//Enables more debugging messages, enable if this script is behaving weird (crashing or throwing NullReference exceptions or something)
//#define NoGUI			//Disables the use of the OnGUI function, can eventually improve performance by a tiny bit

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Pathfinding;

[AddComponentMenu ("Pathfinding/Pathfinder")]
/** Main Pathfinding System.
 * This class handles all the pathfinding system, calculates all paths and stores the info.\n
 * This class is a singleton class, meaning it should only exist at most one active instance of it in the scene.\n
 * It might be a bit hard to use directly, usually interfacing with the pathfinding system is done through the Seeker class.
 * 
 * \ingroup relevant */
public class AstarPath : MonoBehaviour {
	
	/** The version number for the A* %Pathfinding Project
	 */
	public static System.Version Version {
		get {
			return new System.Version (3,0,9);
		}
	}
	
	public enum AstarDistribution { WebsiteDownload, AssetStore };
	
	/** Used by the editor to guide the user to the correct place to download updates */
	public const AstarDistribution Distribution = AstarDistribution.WebsiteDownload;
	
	/** Used by the editor to show some Pro specific stuff.
	 * Note that setting this to true will not grant you any additional features */
	public static readonly bool HasPro = false;
	
	/** \brief See Pathfinding::AstarData */
	public System.Type[] graphTypes {
		get {
			return astarData.graphTypes;
		}
	}
	
	/** Link to the Pathfinding::AstarData object for this graph. The AstarData object stores information about all graphs. */
	public AstarData astarData;
	
	/** Shortcut to Pathfinding::AstarData::graphs */
	public NavGraph[] graphs {
		get {
			if (astarData == null) {
				astarData = new AstarData ();
				//astarData.active = this;
			}
			return astarData.graphs;
		}
		set {
			if (astarData == null) {
				astarData = new AstarData ();
				//astarData.active = this;
			}
			astarData.graphs = value;
		}
	}
	
	//Just some variables which will only be used in the editor 
	
	/** Shows or hides graph inspectors */
	public bool showGraphs = false;
	
	/** Toggle for showing the gizmo debugging for the graphs in the scene view (editor only). */
	public bool showNavGraphs = true;
	
	/** Toggle to show unwalkable nodes.
	  * \see unwalkableNodeDebugSize */
	public bool showUnwalkableNodes = true;
	
	/** If true, all graphs will be scanned in Awake. This does not include loading from the cache. If you disable this, you will have to call \link Scan AstarPath.active.Scan () \endlink yourself to enable pathfinding, alternatively you could load a saved graph from a file. */
	public bool scanOnStartup = true;
	
	public bool prioritizeGraphs = false;
	
	public float prioritizeGraphsLimit = 1F;
	
	/** Link to the color settings for this AstarPath object. Color settings include for example which color the nodes should be in, in the sceneview. */
	public AstarColor colorSettings;
	
	/** How many paths has been computed this run. From application start.\n
	 * Debugging variable */
	public static int PathsCompleted = 0;
	
	public static System.Int64 				TotalSearchedNodes = 0;
	public static System.Int64			 	TotalSearchTime = 0;
	
	/** How long time did the last scan take to complete? Used to prevent automatically rescanning the graphs too often (editor only) */
	public float lastScanTime = 0F;
	
	/** Called on Awake before anything else is done.
	  * This is called at the start of the Awake call, right after #active has been set, but this is the only thing that has been done.\n
	  * Use this when you want to set up default settings for an AstarPath component created during runtime.
	  * \code
	  * //Create a new AstarPath object on Start and apply some default settings
	  * public void Start () {
	  * 	AstarPath.OnAwakeSettings += ApplySettings;
	  * 	AstarPath astar = AddComponent<AstarPath>();
	  * }
	  * 
	  * public void ApplySettings () {
	  * 	//Unregister from the delegate
	  * 	AstarPath.OnAwakeSettings -= ApplySettings;
	  * 	
	  * 	//For example useMultithreading should not be changed after the Awake call
	  * 	//so here's the only place to set it if you create the component during runtime
	  * 	AstarPath.active.useMultithreading = true;
	  * }
	  * \endcode
	  */
	public static OnVoidDelegate OnAwakeSettings;
	
	public static OnGraphDelegate OnGraphPreScan; /**< Called for each graph before they are scanned */
	
	public static OnGraphDelegate OnGraphPostScan; /**< Called for each graph after they have been scanned. All other graphs might not have been scanned yet. */
	
	public static OnPathDelegate OnPathPreSearch; /**< Called for each path before searching. Be carefull when using multithreading since this will be called from a different thread. */
	public static OnPathDelegate OnPathPostSearch; /**< Called for each path after searching. Be carefull when using multithreading since this will be called from a different thread. */
	
	public static OnScanDelegate OnPreScan; /**< Called before starting the scanning */
	public static OnScanDelegate OnPostScan; /**< Called after scanning. This is called before applying links and flood-filling the graphs. */
	public static OnScanDelegate OnLatePostScan; /**< Called after scanning has completed fully. This is called as the last thing in the Scan function. */
	
	public static OnScanDelegate OnGraphsUpdated; /**< Called when any graphs are updated. Register to for example recalculate the path whenever a graph changes. */
	
	/** Called when \a pathID overflows 65536.
	 * The Pathfinding::CleanupPath65K will be added to the queue, and directly after, this callback will be called.
	 * \note This callback will be cleared every timed it is called, so if you want to register to it repeatedly, register to it directly on receiving the callback as well. 
	 */
	public static OnVoidDelegate On65KOverflow; 
	
	/** Will send a callback when it is safe to update the nodes. Register to this with RegisterSafeNodeUpdate
	 * When it is safe is defined as between the path searches.
	 * This callback will only be sent once and is nulled directly after the callback is sent
	 * \warning Note that these callbacks are not thread safe when using multithreading, DO NOT call any part of the Unity API from these callbacks except for Debug.Log
	 */
	public static OnVoidDelegate OnSafeNodeUpdate;
	
	//private static AstarPath _active; /*< Private holder for the active AstarPath object in the scene */
	//private static bool isActiveSet = false; /*< Returns true if #active is not null. This is needed since when using multithreading it will cause an exception to compare an AstarPath object with null */
	
	public float startUpdate = 0;
	
	/** Used to update the graphs internally. Do not use this. Use #OnGraphsUpdated instead */
	public static OnPathDelegate OnGraphUpdate;
	
	/** Stack containing all waiting graph update queries. Add to this stack by using \link UpdateGraphs \endlink
	 * \see UpdateGraphs
	 */
	[System.NonSerialized]
	public Queue<GraphUpdateObject> graphUpdateQueue;
	
	/** Stack used for flood-filling the graph. It is saved to minimize memory allocations. */
	[System.NonSerialized]
	public Stack<Node> floodStack;
	
	/** The last area index which was used. Used for the \link FloodFill(Node node) FloodFill \endlink function to start flood filling with an unused area.
	 * \see FloodFill(Node node)
	 */
	public int lastUniqueAreaIndex = 0;
	
	/** The heuristic to use. The heuristic, often referred to as 'H' is the estimated cost from a node to the target.
	 * \see Pathfinding::Heuristic
	 */
	public Heuristic heuristic = Heuristic.Euclidean;
	
	/** The scale of the heuristic. If a smaller value than 1 is used, the pathfinder will search more nodes (slower).
	 * If 0 is used, the pathfinding will be equal to dijkstra's algorithm.
	 * If a value larger than 1 is used the pathfinding will (usually) be faster because it expands fewer nodes, but the paths might not longer be optimal
	 */
	public float heuristicScale = 1F;
	
	/** The path to debug using gizmos. This is equal to the last path which was calculated, it is used in the editor to draw debug information using gizmos.*/
	public Path debugPath;
	
	/** This is the debug string from the last completed path. The variable will be updated if logPathResults == PathLog.InGame */
	public string inGameDebugPath;
	
	/** The mode to use for drawing nodes in the sceneview.
	 * \see Pathfinding::GraphDebugMode
	 */
	public GraphDebugMode debugMode;
	
	/** Low value to use for certain #debugMode modes. For example if #debugMode is set to G, this value will determine when the node will be totally red.
	 * \see #debugRoof
	 */
	public float debugFloor = 0;
	
	/** High value to use for certain #debugMode modes. For example if #debugMode is set to G, this value will determine when the node will be totally green.
	 * \see #debugFloor
	 */
	public float debugRoof = 10000;
	
	/** If enabled, nodes will draw a line to their 'parent'. This will show the search tree in a clear way. This is editor only.
	 * \todo Add a showOnlyLastPath flag to indicate whether to draw every node or only the ones visited by the latest path.
	 */
	public bool	showSearchTree = false;
	
	/** Size of the red cubes shown in place of unwalkable nodes.
	  * \see showUnwalkableNodes */
	public float unwalkableNodeDebugSize = 0.3F;
	
	/** If enabled, only one node will be searched per search iteration (frame). Used for debugging \note Might not apply for all path types */
	public bool stepByStep = true;
	
	/** Disables or enables new paths to be added to the queue.
	  * Setting this to false also makes the pathfinding thread (if using multithreading) to abort as soon as possible.
	  * It is used when OnDestroy is called to abort the pathfinding thread. */
	private bool acceptNewPaths = true;
	
	/** Should multithreading be enabled. Multithreading puts pathfinding in another thread, this is great for performance on 2+ core computers since the framerate will barely be affected by the pathfinding at all.
	 * But this can cause strange errors and pathfinding stopping to work if you are not carefull (that is, if you are modifying the pathfinding scripts). For basic usage (not modding the pathfinding core) it should be safe.\n
	 * \astarpro
	 */
	//Note - Changing this variable here to 'true' will not grant you additional features, however you might get a bunch of errors
	public readonly bool useMultithreading = false;
	
	public static Path pathQueueEnd; /**< Last path added to the pathfinding queue*/
	
	public static Path pathQueueStart; /**< The next path to be computed (or is being computed)*/
	
	/** The next path to return using multithreading.
	 * The paths have to be returned using a coroutine running on the same thread as the rest of the game to be able to use the Unity API.
	 * This is the next path in the queue to return.*/
	public static Path pathReturnQueueStart;
	
	/** Pop the pathfinding queue once when starting pathfinding. When the pathfinding coroutine or thread terminates because no path queries were sent, the path queue will have to be popped once before pathfinding is started next time the pathfinding starts*/
	public static bool missedPathQueueIncrement = false;
	
	/** Patfinding thread when using multithreading */
	[System.NonSerialized]
	public static Thread activeThread;
	
	/** Max number of milliseconds to spend each frame for pathfinding. At least 100 nodes will be searched each frame (if there are that many to search).
	 * When using multithreading this value is quite irrelevant, but do not set it too low since that could add upp to some overhead */
	public float maxFrameTime = 1F;
	
	/** Max number of iterations in the pathfinding thread without anything to do before aborting the thread.
	 * This is not related to time or game frames in any way, the thread will simply check if there is any work to do every millisecond or something
	 * though after half of #threadTimeoutFrames it will wait 2 ms between each check instead.
	 * After this limit has been reached, the pathfinding thread will abort and will be started again when a new pathfinding call is made
	 * \todo Expose in inspector */
	public static int threadTimeoutFrames = 2000;
	
	/** True if the pathfinding is running. That is, if the \link CalculatePaths \endlink coroutine or the \link CalculatePathsThreaded \endlink thread is running at the moment.*/
	public static bool isCalculatingPaths = false;
	
	
	/** The open list. A binary heap holds and sorts the open list for the pathfinding. Binary Heaps are extreamly fast in providing a priority queue for the node with the lowest F score.*/
	[System.NonSerialized]
	public BinaryHeap binaryHeap;
	
	/** The max size of the binary heap. A good value is from 80% of the number of nodes in the graphs (for small graphs) to 20% of the number of nodes in the graphs (for large graphs).
	 * You will get warning messages if too many nodes are pushed to the open list.
	 * \see #binaryHeap
	 * \todo Add automatic setting (for example, use 40% of the number of nodes in all graphs)
	 */
	public int binaryHeapSize = 5000;
	
	/** Recycle paths to reduce memory allocations. This will put paths in a pool to be reused over and over again. If you use this, your scripts using tht paths should copy the vectorPath array and node array (if used) because when the path is recycled, those arrays will be replaced. I.e you should not get data from it using myPath.someVariable (except when you get the path callback) because 'someVariable' might be changed when the path is recycled. */
	public bool recyclePaths = false;
	
	/** Defines the minimum amount of nodes in an area. If an area has less than this amount of nodes, the area will be flood filled again with the area ID 254, it shouldn't affect pathfinding in any significant way.
	  * Can be found in A* Inspector-->Settings-->Min Area Size
	  */
	public int minAreaSize = 10;
	
	/** Synchronises pathfinding thread and Unity thread for graph updates. When using multithreading, the pathfinding thread will lock on this object when a path request is started and release it when the path has been computed, when updating graphs, the Unity thread will also lock on this object which will cause it to wait until the current path has been computed. This usually results in a break in the pathfinding thread for a few milliseconds, that's why it can be good to limit the number of graph updates per second.
	  * \see #limitGraphUpdates
	  */
	[System.NonSerialized]
	public static System.Object lockObject = new System.Object ();
	
	
	/* The active AstarPath object in the scene */
	//public static AstarPath _active;
	/* Is #_active set to 'null' */
	//public static bool _activeSet = false;
	
	/** Returns the active AstarPath object in the scene.*/
	public new static AstarPath active;
	/*public new static AstarPath active {
		get {
			/*if (!isActiveSet) {
				_active = GameObject.FindObjectOfType (typeof(AstarPath)) as AstarPath;
				//if (_active == null) {
					//Debug.LogError ("No AstarPath object in the scene");
				//}
			}*
			if (_activeSet) {
				return _active;
			} else {
				return null;
			}
		}
		set {
			_activeSet = value != null;	
			_active = value;
			
			/*if (value != null) {
				isActiveSet = true;
				_active = value;
			} else {
				isActiveSet = false;
				_active = value;
			}*
		}
	}*/
	
	/** The amount of debugging messages. Use less debugging to improve performance (a bit) or just to get rid of the Console spamming, use more debugging (heavy) if you want more information about what the pathfinding is doing. InGame will display the latest path log using in game GUI. */
	public PathLog logPathResults = PathLog.Normal;
	
	/*public int AssignNodes (Node[] graphNodes) {
		Node[] tmp = new Node[nodes.Length+graphNodes.Length];
		
		for (int i=0;i<nodes.Length;i++) {
			tmp[i] = nodes[i];
		}
		
		for (int i=0;i<graphNodes.Length;i++) {
			tmp[i+nodes.Length] = graphNodes[i];
		}
		
		int l = nodes.Length;
		
		nodes = tmp;
		
		return l;
	}*/
	
	/** Stack to hold paths waiting to be recycled */
	public static Stack<Path> PathPool;
	
	/** The next unused Path ID. Incremented for every call to GetFromPathPool */
	private int nextFreePathID = 1;
	
	/** Get a path from the Path Pool. If path recycling is off, this will create an entirely new path */
	public static Path GetFromPathPool () {
		
		if (active == null) {
			throw new System.NullReferenceException ("There is no active AstarPath object in the scene");
		}
		
		if (active.recyclePaths) {
			if (PathPool == null) {
				PathPool = new Stack<Path> (10);
				return new Path ();
			}
			
			if (PathPool.Count > 0) {
				Path p = PathPool.Pop ();
				return p;
			} else {
				return new Path ();
			}
		} else {
			return new Path ();
		}
	}
	
	/** Adds the path to the #PathPool. Paths are pooled to reduce memory allocations */
	public static void AddToPathPool (Path p) {
		if (!active.recyclePaths) {
			return;
		}
		
		if (PathPool == null) {
			PathPool = new Stack<Path> (16);
		}
		PathPool.Push (p);
	}
	
	/** Returns the next free path ID. If the next free path ID overflows 65535, a cleanup operation is queued
	  * \see Pathfinding::CleanupPath65K */
	public int GetNextPathID () {
		if (nextFreePathID > 65535) {
			nextFreePathID = 1;
			
			//Queue a cleanup operation to zero all path IDs
			StartPath (new CleanupPath65K ());
			
			int toBeReturned = nextFreePathID++;
			
			if (On65KOverflow != null) {
				OnVoidDelegate tmp = On65KOverflow;
				On65KOverflow = null;
				tmp ();
			}
			
			return toBeReturned;
		}
		return nextFreePathID++;
	}
	
	/** \todo Remove */
	public static PathfindingStatus pathfindingStatus = PathfindingStatus.None;
	
	/** \todo Remove */
	public enum PathfindingStatus {
		None,
		Searching,
		Waiting,
		Locking,
		SearchLoop
	}
	
	/** \todo Remove */
	public static void SetPathfindingStatus (PathfindingStatus s) {
		pathfindingStatus = s;
	}
	
	/** Used to enable gizmos in editor scripts */
	public OnVoidDelegate OnDrawGizmosCallback;
	
	/** Calls OnDrawGizmos on graph generators and also #OnDrawGizmosCallback */
	public void OnDrawGizmos () {
		if (active == null) {
			active = this;
		} else if (active != this) {
			return;
		}
		
		if (graphs == null) { return; }
		
		for (int i=0;i<graphs.Length;i++) {
			if (graphs[i] == null) {continue; }
			graphs[i].OnDrawGizmos (showNavGraphs);
		}
		
		if (showUnwalkableNodes && showNavGraphs) {
			Gizmos.color = AstarColor.UnwalkableNode;
			for (int i=0;i<graphs.Length;i++) {
				if (graphs[i] == null || graphs[i].nodes == null) {continue; }
				Node[] nodes = graphs[i].nodes;
				for (int j=0;j<nodes.Length;j++) {
					if (!nodes[j].walkable) {
						Gizmos.DrawCube (nodes[j].position, Vector3.one*unwalkableNodeDebugSize);
					}
				}
			}
		}
		
		if (OnDrawGizmosCallback != null) {
			OnDrawGizmosCallback ();
		}
	}
	
	/** Draws the InGame debugging (if enabled), also shows the fps if 'L' is pressed down.
	 * \see #logPathResults PathLog
	 */
	public void OnGUI () {
		
		if (Input.GetKey ("l")) {
			GUI.Label (new Rect (Screen.width-100,5,100,25),(1F/Time.smoothDeltaTime).ToString ("0")+" fps");
		}
		
		if (logPathResults == PathLog.InGame) {
			
			if (inGameDebugPath != "") {
						
				GUI.Label (new Rect (5,5,400,600),inGameDebugPath);
			}
		}
		
		/*if (GUI.Button (new Rect (Screen.width-100,5,100,20),"Load New Level")) {
			Application.LoadLevel (0);
		}*/
		
	}
	
#line hidden
	/** Logs a string while taking into account #logPathResults */
	public static void AstarLog (string s) {
		if (active == null) {
			Debug.Log ("No AstarPath object was found : "+s);
			return;
		}
		
		if (active.logPathResults != PathLog.None && active.logPathResults != PathLog.OnlyErrors) {
			Debug.Log (s);
		}
	}
	
	/** Logs an error string while taking into account #logPathResults */
	public static void AstarLogError (string s) {
		if (active == null) {
			Debug.Log ("No AstarPath object was found : "+s);
			return;
		}
		
		if (active.logPathResults != PathLog.None) {
			Debug.LogError (s);
		}
	}
#line default

	/** Prints path results to the log. What it prints can be controled using #logPathResults.
	 * \see #logPathResults \n PathLog
	 * \todo Use string builder instead for lower memory footprint
	 */
	public void LogPathResults (Path p) {
		
		//string debug = "";
		
		if (logPathResults == PathLog.None || (logPathResults == PathLog.OnlyErrors && !p.error)) {
			return;
		}
		
		string debug = p.DebugString (logPathResults);
		
		if (logPathResults == PathLog.InGame) {
			inGameDebugPath = debug;
		} else {
			Debug.Log (debug);
		}
		
		//Add stuff to the log
		/*if (active.logPathResults == PathLog.Normal || logPathResults == PathLog.OnlyErrors) {
			
			if (p.error) {
				debug = "Path Failed : Computation Time: "+(p.duration).ToString ("0.00")+" ms Searched Nodes "+p.searchedNodes+"\nPath number: "+PathsCompleted+"\nError: "+p.errorLog;
			} else {
				debug = "Path Completed : Computation Time: "+(p.duration).ToString ("0.00")+" ms Path Length "+(p.path == null ? "Null" : p.path.Length.ToString ()) + " Searched Nodes "+p.searchedNodes+"\nSmoothed path length "+(p.vectorPath == null ? "Null" : p.vectorPath.Length.ToString ())+"\nPath number: "+p.pathID;
			}
			
		} else if (logPathResults == PathLog.Heavy || logPathResults == PathLog.InGame || logPathResults == PathLog.OnlyErrors) {
			
			if (p.error) {
				debug = "Path Failed : Computation Time: "+(p.duration).ToString ("0.000")+" ms Searched Nodes "+p.searchedNodes+"\nPath number: "+PathsCompleted+"\nError: "+p.errorLog;
			} else {
				debug = "Path Completed : Computation Time: "+(p.duration).ToString ("0.000")+" ms\nPath Length "+(p.path == null ? "Null" : p.path.Length.ToString ()) + "\nSearched Nodes "+p.searchedNodes+"\nSearch Iterations (frames) "+p.searchIterations+"\nSmoothed path length "+(p.vectorPath == null ? "Null" : p.vectorPath.Length.ToString ())+"\nEnd node\n	G = "+p.endNode.g+"\n	H = "+p.endNode.h+"\n	F = "+p.endNode.f+"\n	Point	"+p.endPoint
				+"\nStart Point = "+p.startPoint+"\n"+"Start Node graph: "+p.startNode.graphIndex+" End Node graph: "+p.endNode.graphIndex+"\nBinary Heap size at completion: "+(p.open == null ? "Null" : p.open.numberOfItems.ToString ())+"\nPath number: "+p.pathID;
			}
			
			/*if (active.logPathResults == PathLog.Heavy) {
				Debug.Log (debug);
			} else {
				inGameDebugPath = debug;
			}*
		}
		
		if (logPathResults == PathLog.Normal || logPathResults == PathLog.Heavy || (logPathResults == PathLog.OnlyErrors && p.error)) {
			Debug.Log (debug);
		} else if (logPathResults == PathLog.InGame) {
			inGameDebugPath = debug;
		}*/
		
		//if ((p.error && logPathResults == PathLog.OnlyErrors) || logPathResults == PathLog.Normal) {
		//		Debug.Log (debug);
		//	}
	}
	
	/* Checks if any graphs need updating. If using multithreading, graph updates can't be called from the main pathfinding function since that's in another thread. So every update we check if any graphs need updating.
	 * The \link CalcpulatePathsThreaded pathfinding function \endlink will set #graphsNeedUpdating to true and then wait with computing paths until #waitFlag has been set to false which indicates that the graphs have been updated
	 * \see CalculatePathsThreaded \nUpdateGraphs \nDoUpdateGraphs
	 */
	/*public void Update () {
		if (graphsNeedUpdating) {
			DoUpdateGraphs ();
			graphsNeedUpdating = false;
			waitFlag = false;
		}
	}*/
	
	/** Limit graph updates. If toggled, graph updates will be executed less often (specified by #maxGraphUpdateFreq).*/
	public bool limitGraphUpdates = true;
	
	/** How often should graphs be updated. If #limitGraphUpdates is true, this defines the minimum amount of seconds between each graph update.*/
	public float maxGraphUpdateFreq = 0.2F;
	
	private float lastGraphUpdate = -9999F;
	private bool graphUpdateRoutineRunning = false;
	
	public OnVoidDelegate OnGraphsWillBeUpdated;
	public OnVoidDelegate OnGraphsWillBeUpdated2;
	
	bool isUpdatingGraphs = false;
	bool isRegisteredForUpdate = false;
	
	public IEnumerator DelayedGraphUpdate () {
		graphUpdateRoutineRunning = true;
		yield return new WaitForSeconds (maxGraphUpdateFreq-(Time.realtimeSinceStartup-lastGraphUpdate));
		
		if (useMultithreading) {
			lock (lockObject) {
				DoUpdateGraphs ();
			}
		} else if (!isRegisteredForUpdate) {
			isRegisteredForUpdate = true;
			OnGraphUpdate += new OnPathDelegate (DoUpdateGraphs);
		}
		graphUpdateRoutineRunning = false;
	}
	
	/** Will applying this GraphUpdateObject result in no possible path between \a n1 and \a n2.
	 * Use this only with basic GraphUpdateObjects since it needs special backup logic, it probably wont work with your own specialized ones.
	 * This function is quite a lot slower than a standart Graph Update, but not so much it will slow the game down.
	 * \note This might return false for small areas even if it would block the path if #minAreaSize is greater than zero (0).
	 * So when using this, it is recommended to set #minAreaSize to 0.
	 * \see AstarPath::GetNearest
	 */
	public bool WillBlockPath (GraphUpdateObject ob, Node n1, Node n2) {
		if (useMultithreading) {
			lock (lockObject) {
				return WillBlockPathInternal (ob,n1,n2);
			}
		} else {
			return WillBlockPathInternal (ob,n1,n2);
		}
	}
	
	private bool WillBlockPathInternal (GraphUpdateObject ob, Node n1, Node n2) {
		if (n1.area != n2.area) return true;
		
		ob.trackChangedNodes = true;
		foreach (IUpdatableGraph g in astarData.GetUpdateableGraphs ()) {
				g.UpdateArea (ob);
		}
		
		FloodFill ();
		
		bool returnVal = n1.area != n2.area;
		
		ob.RevertFromBackup ();
		FloodFill ();
		
		return returnVal;
	}
	
	/** Returns if there is a walkable path from \a n1 to \a n2.
	 * If you are making changes to the graph, areas must first be recaculated using FloodFill()
	 * \note This might return true for small areas even if there is no possible path if #minAreaSize is greater than zero (0).
	 * So when using this, it is recommended to set #minAreaSize to 0. */
	public static bool IsPathPossible (Node n1, Node n2) {
		return n1.area == n2.area;
	}
	
	/** Update all graphs within \a bounds after \a t seconds. This function will add a GraphUpdateObject to the #graphUpdateQueue. The graphs will be updated before the next path is calculated.
	 * \see Update
	 * \see DoUpdateGraphs
	 */
	public void UpdateGraphs (Bounds bounds, float t) {
		UpdateGraphs (new GraphUpdateObject (bounds),t);
	}
	
	/** Update all graphs using the GraphUpdateObject after \a t seconds. This can be used to, e.g make all nodes in an area unwalkable, or set them to a higher penalty.
	*/
	public void UpdateGraphs (GraphUpdateObject ob, float t) {
		StartCoroutine (UpdateGraphsInteral (ob,t));
	}
	
	/** pdate all graphs using the GraphUpdateObject after \a t seconds */
	private IEnumerator UpdateGraphsInteral (GraphUpdateObject ob, float t) {
		yield return new WaitForSeconds (t);
		UpdateGraphs (ob);
	}
	
	/** Update all graphs within \a bounds. This function will add a GraphUpdateObject to the #graphUpdateQueue. The graphs will be updated before the next path is calculated.
	 * \see Update
	 * \see DoUpdateGraphs
	 */
	public void UpdateGraphs (Bounds bounds) {
		UpdateGraphs (new GraphUpdateObject (bounds));
	}
	
	/** Update all graphs using the GraphUpdateObject. This can be used to, e.g make all nodes in an area unwalkable, or set them to a higher penalty.
	*/
	public void UpdateGraphs (GraphUpdateObject ob) {
		//Debug.LogWarning ("Trying To Update Graphs");
		
		if (graphUpdateQueue == null) {
			graphUpdateQueue = new Queue<GraphUpdateObject> ();
		}
		
		graphUpdateQueue.Enqueue (ob);
		
		if (isUpdatingGraphs) {
			return;
		}
		
		if (limitGraphUpdates && Time.realtimeSinceStartup-lastGraphUpdate < maxGraphUpdateFreq) {
			if (!graphUpdateRoutineRunning) {
				StartCoroutine (DelayedGraphUpdate ());
			}
		} else {
			if (useMultithreading) {
				lock (lockObject) {
					DoUpdateGraphs ();
				}
			} else if (!isRegisteredForUpdate) {
				//Only add a callback for the first item
				isRegisteredForUpdate = true;
				OnGraphUpdate += DoUpdateGraphs;
			}
		}
		
	}
	
	/** Receives callback from #OnGraphUpdate
	  * \see DoUpdateGraphs
	  */
	private void DoUpdateGraphs (Path p) {
		OnGraphUpdate -= new OnPathDelegate (DoUpdateGraphs);
		isRegisteredForUpdate = false;
		DoUpdateGraphs ();
	}
	
	/** Updates the graphs based on the #graphUpdateQueue
	 * \see UpdateGraphs
	 */
	private void DoUpdateGraphs () {
		
		isUpdatingGraphs = true;
		lastGraphUpdate = Time.realtimeSinceStartup;
		
		if (OnGraphsWillBeUpdated2 != null) {
			OnVoidDelegate callbacks = OnGraphsWillBeUpdated2;
			OnGraphsWillBeUpdated2 = null;
			callbacks ();
		}
		
		if (OnGraphsWillBeUpdated != null) {
			OnVoidDelegate callbacks = OnGraphsWillBeUpdated;
			OnGraphsWillBeUpdated = null;
			callbacks ();
		}
		
		//If any GUOs requires a flood fill, then issue it, otherwise we can skip it to save processing power
		bool anyRequiresFloodFill = false;
		
		if (graphUpdateQueue != null) {
			while (graphUpdateQueue.Count > 0) {
				GraphUpdateObject ob = graphUpdateQueue.Dequeue ();
				
				if (ob.requiresFloodFill) anyRequiresFloodFill = true;
				
				foreach (IUpdatableGraph g in astarData.GetUpdateableGraphs ()) {
						
						g.UpdateArea (ob);
				}
				
			}
		}
		isUpdatingGraphs = false;
		
		if (anyRequiresFloodFill) {
			FloodFill ();
		}
		
		if (OnGraphsUpdated != null) {
			OnGraphsUpdated (this);
		}
		
		//Debug.Log ("Updating Graphs... "+((Time.realtimeSinceStartup-startUpdate)*1000).ToString ("0.00"));
		//resetEvent.Set ();
		//resetFlag = true;
	}
	
	public void RegisterCanUpdateGraphs (OnVoidDelegate callback, OnVoidDelegate callback2 = null) {
		
		OnGraphsWillBeUpdated += callback;
		
		if (callback2 != null) {
			OnGraphsWillBeUpdated2 += callback2;
		}
		
		if (limitGraphUpdates && Time.realtimeSinceStartup-lastGraphUpdate < maxGraphUpdateFreq) {
			if (!graphUpdateRoutineRunning) {
				StartCoroutine (DelayedGraphUpdate ());
			}
		} else {
			if (useMultithreading) {
				lock (lockObject) {
					DoUpdateGraphs ();
				}
			} else if (!isRegisteredForUpdate) {
				//Only add a callback for the first item
				isRegisteredForUpdate = true;
				OnGraphUpdate += DoUpdateGraphs;
			}
		}
	}
	
	//[ContextMenu("Log Profiler")]
	public void LogProfiler () {
		//AstarProfiler.PrintFastResults ();
		
	}
	
	//[ContextMenu("Reset Profiler")]
	public void ResetProfiler () {
		//AstarProfiler.Reset ();
	}
	
	/** Sets up all needed variables and scanns the graphs. Calls Initialize, starts the ReturnPaths coroutine and scanns all graphs.
	 * \see #OnAwakeSettings */
	public void Awake () {
		
		active = this;
		
		//Disable GUILayout to gain some performance, it is not used in the OnGUI call
		useGUILayout = false;
		
		if (OnAwakeSettings != null) {
			OnAwakeSettings ();
		}
		
		Initialize ();
		
		
		if (scanOnStartup) {
			if (!astarData.cacheStartup || astarData.data_cachedStartup == null) {
				Scan ();
			}
		}
	}
	
	/** Makes sure #active is set to this object and that #astarData is not null. Also calls OnEnable for the #colorSettings */
	public void SetUpReferences () {
		active = this;
		if (astarData == null) {
			astarData = new AstarData ();
		}
		
		if (astarData.userConnections == null) {
			astarData.userConnections = new UserConnection[0];
		}
		
		if (colorSettings == null) {
			colorSettings = new AstarColor ();
		}
			
		colorSettings.OnEnable ();
	}
	
	/** Initializes various variables. \link SetUpReferences Sets up references \endlink, \link AstarData.FindGraphTypes searches for graph types \endlink, creates the #binaryHeap and calls Awake on #astarData and on all graphs */
	public void Initialize () {
		
		AstarProfiler.InitializeFastProfile (new string [14] {
			"Prepare", 			//0
			"Initialize",		//1
			"CalculateStep",	//2
			"Trace",			//3
			"Open",				//4
			"UpdateAllG",		//5
			"Add",				//6
			"Remove",			//7
			"PreProcessing",	//8
			"Callback",			//9
			"Overhead",			//10
			"Log",				//11
			"ReturnPaths",		//12
			"PostPathCallback"	//13
		});
		
		SetUpReferences ();
		
		astarData.FindGraphTypes ();
		binaryHeap = new BinaryHeap (binaryHeapSize);
		
		//astarData.DeserializeGraphs (new AstarSerializer (this));
		astarData.Awake ();
		
		for (int i=0;i<astarData.graphs.Length;i++) {
			astarData.graphs[i].Awake ();
		}
	}
	
	/** Clears up variables and other stuff, destroys graphs */
	public void OnDestroy () {
		
		if (logPathResults == PathLog.Heavy)
			Debug.Log ("+++ Destroyed - Cleaning Up Pathfinding Data +++");
		
		
		//Don't accept any more path calls to this AstarPath instance. This will also cause the eventual multithreading thread to abort
		acceptNewPaths = false;
		
		//Stop the multithreading thread
		if (activeThread != null) {
			
			if (!activeThread.Join (100)) {
				Debug.LogError ("Could not terminate pathfinding thread properly, trying Thread.Abort");
				activeThread.Abort ();
			}
			
			activeThread = null;
		}
		
		//Must be set before the OnDestroy calls are made, since they might call RegisterSafeNodeUpdate, and if it set to true then the callback will never be called
		isCalculatingPaths		 = false;
		
		if (logPathResults == PathLog.Heavy)
			Debug.Log ("Destroying Graphs");

		
		//Clear graphs up
		if (astarData.graphs != null) {
			for (int i=0;i<astarData.graphs.Length;i++) {
				astarData.graphs[i].OnDestroy ();
			}
		}
		astarData.graphs = null;
		
		if (logPathResults == PathLog.Heavy)
			Debug.Log ("Returning Paths");
		
		//Return all paths with errors
		
		if (useMultithreading) {
			while (pathReturnQueueStart != null) {
				Path p = pathReturnQueueStart;
				p.error = true;
				p.errorLog = "Canceled because AstarPath object was destroyed";
				p.ReturnPath ();
				
				if (p.next != null) {
					pathReturnQueueStart = p.next;
				} else {
					break;
				}
			}
		} else {
			while (pathQueueStart != null) {
				Path p = pathQueueStart;
				p.error = true;
				p.errorLog = "Canceled because AstarPath object was destroyed";
				p.ReturnPath ();
				
				if (p.next != null) {
					pathQueueStart = p.next;
				} else {
					break;
				}
			}
		}
		
		if (pathQueueStart != null) {
			pathQueueStart.next = null;
			pathQueueStart.error = true;
			pathQueueStart.callback = null;
		}
		
		if (logPathResults == PathLog.Heavy)
			Debug.Log ("Cleaning up variables");
		
		//Clear variables up, static variables are good to clean up, otherwise the next scene might get weird data
		floodStack = null;
		graphUpdateQueue = null;
		//astarData = null;
		binaryHeap = null;
		
		pathQueueEnd			 = null;
		pathQueueStart			 = null;
		pathReturnQueueStart	 = null;
		missedPathQueueIncrement = false;
		
		OnGraphPreScan			= null;
		OnGraphPostScan			= null;
		OnPathPreSearch			= null;
		OnPathPostSearch		= null;
		OnPreScan				= null;
		OnPostScan				= null;
		OnGraphUpdate			= null;
		OnGraphsUpdated			= null;
		
		PathsCompleted = 0;
		
		//isActiveSet = false;
		active = null;
		
	}
	
	/** Floodfills starting from the specified node */
	public void FloodFill (Node seed) {
		FloodFill (seed, lastUniqueAreaIndex+1);
		lastUniqueAreaIndex++;
	}
	
	/** Floodfills starting from 'seed' using the specified area */
	public void FloodFill (Node seed, int area) {
		
		if (area > 255) {
			Debug.LogError ("Too high area index - The maximum area index is 255");
			return;
		}
		
		if (area < 0) {
			Debug.LogError ("Too low area index - The minimum area index is 0");
			return;
		}
					
		if (floodStack == null) {
			floodStack = new Stack<Node> (1024);
		}
		
		Stack<Node> stack = floodStack;
					
		stack.Clear ();
		
		stack.Push (seed);
		seed.area = area;
		
		while (stack.Count > 0) {
			stack.Pop ().FloodFill (stack,area);
		}
				
	}
	
	/** Floodfills all graphs and updates areas for every node.
	  * \see Pathfinding::Node::area */
	public void FloodFill () {
		
		
		if (astarData.graphs == null) {
			return;
		}
		
		int area = 0;
		
		lastUniqueAreaIndex = 0;
		
		if (floodStack == null) {
			floodStack = new Stack<Node> (1024);
		}
		
		Stack<Node> stack = floodStack;
		
		for (int i=0;i<graphs.Length;i++) {
			NavGraph graph = graphs[i];
			
			if (graph.nodes != null) {
				for (int j=0;j<graph.nodes.Length;j++) {
					graph.nodes[j].area = 0;
				}
			}
		}
		
		int smallAreasDetected = 0;
		
		for (int i=0;i<graphs.Length;i++) {
			
			NavGraph graph = graphs[i];
			
			if (graph.nodes == null) {
				Debug.LogWarning ("Graph "+i+" has not defined any nodes");
				continue;
			}
			
			for (int j=0;j<graph.nodes.Length;j++) {
				if (graph.nodes[j].walkable && graph.nodes[j].area == 0) {
					
					area++;
					
					if (area > 255) {
						Debug.LogError ("Too many areas - The maximum number of areas is 256");
						area--;
						break;
					}
					
					stack.Clear ();
					
					stack.Push (graph.nodes[j]);
					
					int counter = 1;
					
					graph.nodes[j].area = area;
					
					while (stack.Count > 0) {
						counter++;
						stack.Pop ().FloodFill (stack,area);
					}
					
					if (counter < minAreaSize) {
						
						//Flood fill the area again with area ID 254, this identifies a small area
						stack.Clear ();
						
						stack.Push (graph.nodes[j]);
						graph.nodes[j].area = 254;
					
						while (stack.Count > 0) {
							stack.Pop ().FloodFill (stack,254);
						}
					
						smallAreasDetected++;
						area--;
					}
				}
			}
		}
		
		lastUniqueAreaIndex = area;
		
		
		if (smallAreasDetected > 0) {
			AstarLog (smallAreasDetected +" small areas were detected (fewer than "+minAreaSize+" nodes)," +
				"these might have the same IDs as other areas, but it shouldn't affect pathfinding in any significant way (you might get All Nodes Searched as a reason for path failure)." +
				"\nWhich areas are defined as 'small' is controlled by the 'Min Area Size' variable, it can be changed in the A* inspector-->Settings-->Min Area Size" +
				"\nThe small areas will use the area id 254");
		}
		
	}
	
#if UNITY_EDITOR
	[UnityEditor.MenuItem ("Component/Pathfinding/Scan %&s")]
	public static void MenuScan () {
		
		if (AstarPath.active == null) {
			AstarPath.active = FindObjectOfType(typeof(AstarPath)) as AstarPath;
			if (AstarPath.active == null) {
				return;
			}
		}
		
		UnityEditor.EditorUtility.DisplayProgressBar ("Scanning","Scanning...",0);
		
		try {
			foreach (Progress progress in AstarPath.active.ScanLoop ()) {
				UnityEditor.EditorUtility.DisplayProgressBar ("Scanning",progress.description,progress.progress);
			}
		} catch (System.Exception e) {
			Debug.LogError ("There was an error generating the graphs:\n"+e.ToString ()+"\n\nIf you think this is a bug, please contact me on arongranberg.com (post a comment)\n");
			UnityEditor.EditorUtility.DisplayDialog ("Error Generating Graphs","There was an error when generating graphs, check the console for more info","Ok");
		} finally {
			UnityEditor.EditorUtility.ClearProgressBar();
		}
	}
#endif
	
	/** Called by editor scripts to rescan the graphs e.g when the user moved a graph */
	public void AutoScan () {
		
		if (!Application.isPlaying && lastScanTime < 0.11F) {
			Scan ();
		}
	}
	
	/** Scanns all graphs */
	public void Scan () {
		IEnumerator<Progress> scanning = ScanLoop ().GetEnumerator ();
		
		while (scanning.MoveNext ()) {
		}
		
	}
	
	/** Scanns all graphs. This is a IEnumerable, you can loop through it to get the progress
	  * \code foreach (Progress progress in AstarPath.active.ScanLoop ()) {
	*	 Debug.Log ("Scanning... " + progress.description + " - " + (progress.progress*100).ToString ("0") + "%");
	  * } \endcode
	  * \see Scan
	  */
	public IEnumerable<Progress> ScanLoop () {
		
		if (graphs == null) {
			yield break;
		}
		
		yield return new Progress (0.02F,"Updating graph shortcuts");
		
		astarData.UpdateShortcuts ();
		
		yield return new Progress (0.05F,"Pre processing graphs");
		
		if (OnPreScan != null) {
			OnPreScan (this);
		}
		
		//float startTime = Time.realtimeSinceStartup;
		System.DateTime startTime = System.DateTime.UtcNow;
		
		for (int i=0;i<graphs.Length;i++) {
			
			NavGraph graph = graphs[i];
			
			if (OnGraphPreScan != null) {
				yield return new Progress (Mathfx.MapTo (0.05F,0.9F,(float)(i+0.5F)/(graphs.Length+1)),"Scanning graph "+(i+1)+" of "+graphs.Length+" - Pre processing");
				OnGraphPreScan (graph);
			}
			
			yield return new Progress (Mathfx.MapTo (0.05F,0.9F,(float)(i+1F)/(graphs.Length+1)),"Scanning graph "+(i+1)+" of "+graphs.Length);
			
			graph.Scan ();
			
			yield return new Progress (Mathfx.MapTo (0.05F,0.9F,(float)(i+1.1F)/(graphs.Length+1)),"Scanning graph "+(i+1)+" of "+graphs.Length+" - Assigning graph indices");
			if (graph.nodes != null) {
				for (int j=0;j<graph.nodes.Length;j++) {
					graph.nodes[j].graphIndex = i;
				}
			}
			
			if (OnGraphPostScan != null) {
				yield return new Progress (Mathfx.MapTo (0.05F,0.9F,(float)(i+1.5F)/(graphs.Length+1)),"Scanning graph "+(i+1)+" of "+graphs.Length+" - Post processing");
				OnGraphPostScan (graph);
			}
			
		}
		
		yield return new Progress (0.9F,"Post processing graphs");
		
		if (OnPostScan != null) {
			OnPostScan (this);
		}
		
		yield return new Progress (0.9F,"Applying links");
		
		ApplyLinks ();
		
		yield return new Progress (0.95F,"Computing areas");
		
		FloodFill ();
		
		yield return new Progress (0.9F,"Late post processing");
		
		if (OnLatePostScan != null) {
			OnLatePostScan (this);
		}
		
		lastScanTime = (float)(System.DateTime.UtcNow-startTime).TotalSeconds;//Time.realtimeSinceStartup-startTime;
		Debug.Log ("Scanning - Process took "+(lastScanTime*1000).ToString ("0")+" ms to complete ");
		
		//for (int i=0;i<graphs.Length;i++) {
		//	(graphs[i] as NavMeshGraph).PostProcess ();
		//}
	}
	
	/** Applies links to the scanned graphs. Called right after #OnPostScan and before #FloodFill(). */
	public void ApplyLinks () {
		for (int i=0;i<astarData.userConnections.Length;i++) {
			UserConnection conn = astarData.userConnections[i];
			
			if (conn.type == ConnectionType.Connection) {
				Node n1 = GetNearest (conn.p1);
				Node n2 = GetNearest (conn.p2);
				
				if (n1 == null || n2 == null) {
					continue;
				}
				
				int cost = conn.doOverrideCost ? conn.overrideCost : (n1.position-n2.position).costMagnitude;
				
				if (conn.enable) {
					n1.AddConnection (n2, cost);
				
					if (!conn.oneWay) {
						n2.AddConnection (n1, cost);
					}
				} else {
					n1.RemoveConnection (n2);
					if (!conn.oneWay) {
						n2.RemoveConnection (n1);
					}
				}
			} else {
				Node n1 = GetNearest (conn.p1);
				if (n1 == null) { continue; }
				
				if (conn.doOverrideWalkability) {
					n1.walkable = conn.enable;
					if (!n1.walkable) {
						n1.UpdateNeighbourConnections ();
						n1.UpdateConnections ();
					}
				}
				
				if (conn.doOverridePenalty) {
					n1.penalty = conn.overridePenalty;
				}
				
			}
		}
	}
	
	/** Will send a callback when it is safe to update nodes. This is defined as between the path searches.
	  * This callback will only be sent once and is nulled directly after the callback has been sent
	  * \warning Note that these callbacks are not thread safe when using multithreading, DO NOT call any part of the Unity API from these callbacks except for Debug.Log
	  * \see RegisterThreadSafeNodeUpdate
	  */
	public static void RegisterSafeNodeUpdate (OnVoidDelegate callback) {
		if (callback == null) {
			return;
		}
		
		if (isCalculatingPaths) {
			OnSafeNodeUpdate += callback;
		} else {
			callback ();
		}
	}
	
	/** Will send a callback when it is safe to update nodes. This is defined as between the path searches.
	  * This callback will only be sent once and is nulled directly after the callback has been sent. This callback is also threadsafe, and because of that, using it often might affect performance when called often and multithreading is enabled due to locking and synchronisation.
	  * \see RegisterSafeNodeUpdate
	  */
	public static void RegisterThreadSafeNodeUpdate (OnVoidDelegate callback) {
		if (callback == null) {
			return;
		}
		
		if (isCalculatingPaths) {
			if (active.useMultithreading) {
				lock (lockObject) {
					callback ();
				}
			} else {
				OnSafeNodeUpdate += callback;
			}
		} else {
			callback ();
		}
	}
	
	public static IEnumerator DelayedPathReturn (Path p) {
		yield return 0;
		p.ReturnPath ();
	}
	
	/** Puts the Path in queue for calculation */
	public static void StartPath (Path p) {
		
		if (active == null) {
			Debug.LogError ("There is no AstarPath object in the scene");
			return;
		}
		
		if (!active.acceptNewPaths) {
			p.error = true;
			p.errorLog += "No new paths are accepted";
			p.ReturnPath ();
			return;
		}
		
		if (active.graphs == null || active.graphs.Length == 0) {
			Debug.LogError ("There are no graphs in the scene");
			p.error = true;
			p.errorLog = "There are no graphs in the scene";
			p.ReturnPath ();
			return;
		}
		
		/*int nextPath = lastAddedPath+1;
		if (nextPath >= active.pathQueueLength) {
			nextPath = 0;
		}
		
		if (nextPath == currentPath) {
			Debug.LogError ("Too many paths in queue");
			return;
		}
		
		pathQueue[nextPath] = p;
		
		if (!isCalculatingPaths) {
			lastAddedPath = nextPath;
			
			activeThread = new Thread (new ThreadStart (CalculatePathsThreaded));
			activeThread.Start ();
			//active.StartCoroutine (CalculatePaths ());
		} else {
			lastAddedPath = nextPath;
		}*/
		
		//@
		//p.callTime = Time.realtimeSinceStartup;
		//@Edit in - System.DateTime startTime = System.DateTime.Now;
		
		if (pathQueueEnd == null) {
			Debug.Log ("Initializing Path Queue...");
			
			pathQueueEnd = p;
			pathQueueStart = p;
			
			if (active.useMultithreading) {
				pathReturnQueueStart = p;
			}
			
		} else {
			
			pathQueueEnd.next = p;
			pathQueueEnd = p;
			
			if (pathQueueStart == null) {
				pathQueueStart = p;
			}
		}
		
			if (!isCalculatingPaths) {
					Debug.Log ("Starting Pathfinder...");
					
					active.StartCoroutine (CalculatePaths ());
			}
			
	}
	

	
	/** Main pathfinding function. This coroutine will calculate the paths in the pathfinding queue.
	 * \see CalculatePathsThreaded ()
	 */
	public static IEnumerator CalculatePaths () {
		isCalculatingPaths = true;
		
		float ptf = 0F;
		
		int framesWithNoPaths = 0;
		
		//The path currently being computed
		Path p = pathQueueStart;
		
		if (missedPathQueueIncrement) {
			
			missedPathQueueIncrement = false;
			
			if (pathQueueStart.next != null) {
				pathQueueStart = pathQueueStart.next;
				p = pathQueueStart;
			} else {
				Debug.LogError ("Error : No path was added to the queue, but the pathfinding was started");
				yield break;
			}
		}	
			
		while (p != null) {
			framesWithNoPaths = 0;
			
			//Wait for a bit if we have calculated a lot of paths
			if (ptf >= active.maxFrameTime) {
				//for (int i=0;i<(active.pathExecutionDelay < 1 ? 1 : active.pathExecutionDelay);i++) {
					yield return 0;
				//}
				ptf = 0F;
			}
			
			bool skip = false;
			if (p.processed) {
				skip = true;
				p.error = true;
				p.errorLog += "Calling pathfinding with an already processed path";
				Debug.LogError (p.errorLog);
			}
			
			if (!skip) {
				
				//Note: These Profiler calls will not get included if the AstarProfiler.cs script does not have the #define DEBUG enabled
				//[Conditional] attributes FTW!
				AstarProfiler.StartFastProfile (8);
				
				if (OnPathPreSearch != null) {
					OnPathPreSearch (p);
				}
				
				if (OnGraphUpdate != null) {
					OnGraphUpdate (p);
				}
				
				if (OnSafeNodeUpdate != null) {
					OnSafeNodeUpdate ();
					OnSafeNodeUpdate = null;
				}
				
				AstarProfiler.EndFastProfile (8);
					
				/*currentPath++;
				if (currentPath >= active.pathQueueLength) {
					currentPath = 0;
				}*/
				
				//Path p = pathQueue[currentPath]; 
				//Path p = pathQueueStarts;
				
				AstarProfiler.StartFastProfile (0);
				
				p.Prepare ();
				
				AstarProfiler.EndFastProfile (0);
				
				if (!p.error) {
					
					if (OnPathPreSearch != null) {
						OnPathPreSearch (p);
					}
				
					//ptf += 0.9F;
					
					//For debug uses, we set the last computed path to p, so we can view debug info on it in the editor (scene view).
					active.debugPath = p;
					
					AstarProfiler.StartFastProfile (1);
					
					p.Initialize ();
					
					AstarProfiler.EndFastProfile (1);
					
					ptf += p.duration+0.1F;
					
					//The error can turn up in the Init function
					if (!p.IsDone ()) {
						
						AstarProfiler.StartFastProfile (2);
						
						ptf += p.CalculateStep (active.maxFrameTime-ptf);
						p.searchIterations++;
						
						AstarProfiler.EndFastProfile (2);
						
						while (!p.IsDone ()) {
							yield return 0;
							
							AstarProfiler.StartFastProfile (2);
							
							//Reset the counter for this frame since we have called yield at least once and has now only computed 1 path this frame
							ptf = p.CalculateStep (active.maxFrameTime);
							p.searchIterations++;
							
							AstarProfiler.EndFastProfile (2);
						}
					}
					
					AstarProfiler.StartFastProfile (13);
					
					if (OnPathPostSearch != null) {
						OnPathPostSearch (p);
					}
					
					AstarProfiler.EndFastProfile (13);
					//1000F));//.ToString ("0.0") +"ms");	  //Return Time: "+((Time.realtimeSinceStartup-p.realStartTime)*1000).ToString ("0.0")+"ms");
					
				} else {
					//0.1 is added to prevent infinite loop when the duration is to short to measure
					ptf += p.duration+0.05F;
				}
				
				
				AstarProfiler.StartFastProfile (9);
					
				PathsCompleted++;
				
				if (p.pathID > 2) {
					TotalSearchedNodes += p.searchedNodes;
					TotalSearchTime += (System.Int64)System.Math.Round (p.duration*10000);
				}
				
				if (OnPathPostSearch != null) {
					OnPathPostSearch (p);
				}
				
				p.processed = true;
				
				p.ReturnPath ();
				
				
				//Add stuff to the log
				active.LogPathResults (p);
			}
			
			AstarProfiler.EndFastProfile (9);
			
			while (pathQueueStart.next == null) {
				ptf = 0;
				
				framesWithNoPaths++;
				
				if (OnSafeNodeUpdate != null) {
					OnSafeNodeUpdate ();
					OnSafeNodeUpdate = null;
				}
				
				if (OnGraphUpdate != null) {
					OnGraphUpdate (p);
				}
				
				if (framesWithNoPaths > 5000) {
					missedPathQueueIncrement = true;
					isCalculatingPaths = false;
					yield break;
				}
			
				yield return 0;
				
			}
			
			Path tmp = pathQueueStart;
			
			pathQueueStart = pathQueueStart.next;
			p = pathQueueStart;
			
			//Remove the reference to prevent possible memory leaks
			//If for example the first path computed was stored somewhere, it would through the linked list contain references to all comming paths to be computed, and thus the nodes those paths searched. Possibly other paths too through the "activePath"/"script" variable on each node. That adds up
			tmp.next = null;
		}
		
		Debug.LogError ("Error : This part should never be reached");
		isCalculatingPaths = false;
	}
		
	/** Returns the nearest node to a position using the specified NNConstraint.
	  * Searches through all graphs for their nearest nodes to the specified position and picks the closest one. */
	public NNInfo GetNearest (Vector3 position, NNConstraint constraint = null, Node hint = null) {
		
		if (graphs == null) { return null; }
		
		if (constraint == null) {
			constraint = NNConstraint.None;
		}
		
		float minDist = float.PositiveInfinity;//Math.Infinity;
		NNInfo nearestNode = new NNInfo ();
		int nearestGraph = 0;
		
		for (int i=0;i<graphs.Length;i++) {
			
			NavGraph graph = graphs[i];
			
			
			NNInfo nnInfo = graph.GetNearest (position, constraint);
			
			Node node = nnInfo.node;
			
			if (node == null) {
				continue;
			}
				
			float dist = ((Vector3)nnInfo.clampedPosition-position).magnitude;
			
			if (prioritizeGraphs && dist < prioritizeGraphsLimit) {
				//The node is close enough, choose this graph and discard all others
				minDist = dist*(int)nnInfo.priority;
				nearestNode = nnInfo;
				nearestGraph = i;
				break;
			} else {
				if (dist*(int)nnInfo.priority < minDist) {
					minDist = dist*(int)nnInfo.priority;
					nearestNode = nnInfo;
					nearestGraph = i;
				}
			}
		}
		
		if (nearestNode.node != null && !constraint.Suitable (nearestNode.node)) {
			
			//Check if a constrained node has already been set
			if (nearestNode.constrainedNode != null) {
				nearestNode.node = nearestNode.constrainedNode;
				nearestNode.clampedPosition = nearestNode.constClampedPosition;
				
			} else {
				//Otherwise, perform a check to force the graphs to check for a suitable node
				NNInfo nnInfo = graphs[nearestGraph].GetNearestForce (position, constraint);
				
				if (nnInfo.node != null) {
					nearestNode = nnInfo;
				}
			}
		}
		
		return nearestNode;
	}
	
	/** Returns the node closest to the ray (slow).
	  * \warning This function is brute-force and is slow, it can barely be used once per frame */
	public Node GetNearest (Ray ray) {
		
		if (graphs == null) { return null; }
		
		float minDist = Mathf.Infinity;
		Node nearestNode = null;
		
		Vector3 lineDirection = ray.direction;
		Vector3 lineOrigin = ray.origin;
		
		for (int i=0;i<graphs.Length;i++) {
			
			NavGraph graph = graphs[i];
		
			Node[] nodes = graph.nodes;
			
			if (nodes == null) {
				continue;
			}
			
			for (int j=0;j<nodes.Length;j++) {
				
				Node node = nodes[j];
	        	Vector3 pos = (Vector3)node.position;
				Vector3 p = lineOrigin+(Vector3.Dot(pos-lineOrigin,lineDirection)*lineDirection);
				
				float tmp = Mathf.Abs (p.x-pos.x);
				tmp *= tmp;
				if (tmp > minDist) continue;
				
				tmp = Mathf.Abs (p.z-pos.z);
				tmp *= tmp;
				if (tmp > minDist) continue;
				
				float dist = (p-pos).sqrMagnitude;
				
				if (dist < minDist) {
					minDist = dist;
					nearestNode = node;
				}
			}
			
		}
		
		return nearestNode;
	}
	    
	//The runtime variable is there to inform the script about that some variables might not get saved, like the sourceMesh reference in the NavmeshGraph which is a reference to the Unity asset database and cannot be saved or loaded during runtime.
	//When runtime is false, the stream won't be closed
	/** Obsolete */
	public AstarSerializer Savex (NavGraph graph, bool runtime) {
		
		float startTime = Time.realtimeSinceStartup;
		
		if (isCalculatingPaths) {
			Debug.LogWarning ("The script is currently calculating paths, the serialization can interfere with the pathfinding");
		}
		
		//Fill some variables with index values for each node which is usefull to the serializer
		//This can interfere with pathfinding so all pathfinding should be stopped before calling this function
		for (int i=0;i<graphs.Length;i++) {
			NavGraph graphx = graphs[i];
			
			if (graphx.nodes == null) {
				continue;
			}
			
			for (int q=0;q<graphx.nodes.Length;q++) {
				graphx.nodes[q].g = q;
				graphx.nodes[q].h = i;
			}
		}
		
		AstarSerializer serializer = new AstarSerializer (this);
		
		serializer.compress = astarData.compress;
		
		//serializer.Serialize (graph,this, runtime);
		
		if (runtime) {
			serializer.Close ();
		}
		
		Debug.Log ("Saving took "+(Time.realtimeSinceStartup-startTime).ToString ("0.00")+" s to complete");
		
		return serializer;
	}
	
	/** Obsolete */
	public AstarSerializer Loadx (bool runtime, out NavGraph graph) {
		return Loadx (runtime, out graph, null);
	}
	
	/** Obsolete */
	public AstarSerializer Loadx (bool runtime, out NavGraph graph, AstarSerializer.DeSerializationInterrupt interrupt) {
		
		float startTime = Time.realtimeSinceStartup;
		
		AstarSerializer serializer = new AstarSerializer (this);
		
		//serializer.DeSerialize (this, runtime, out graph,interrupt);
		
		if (runtime) {
			serializer.Close ();
		}
		
		graph = null;
		Debug.Log ("Loading took "+(Time.realtimeSinceStartup-startTime)+" s to complete");
		return serializer;
	}
	
}
