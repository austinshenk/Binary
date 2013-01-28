using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pathfinding {
	
	/** Basic point graph.
	 * \ingroup graphs
	  * The List graph is the most basic graph structure, it consists of a number of interconnected points in space, waypoints or nodes.\n
	  * The list graph takes a Transform object as "root", this Transform will be searched for child objects, every child object will be treated as a node. It will then check if any connections between the nodes can be made, first it will check if the distance between the nodes isn't too large ( #maxDistance ) and then it will check if the axis aligned distance isn't too high. The axis aligned distance, named #limits, is useful because usually an AI cannot climb very high, but linking nodes far away from each other, but on the same Y level should still be possible. #limits and #maxDistance won't affect anything if the values are 0 (zero) though. \n
	  * Lastly it will check if there are any obstructions between the nodes using <a href="http://unity3d.com/support/documentation/ScriptReference/Physics.Raycast.html">raycasting</a> which can optionally be thick.\n
	  * One thing to think about when using raycasting is to either place the nodes a small distance above the ground in your scene or to make sure that the ground is not in the raycast \a mask.\n
	  * \note Does not support linecast because of obvious reasons.
	  */
	public class ListGraph : NavGraph, ISerializableGraph
	{
		
		/** Childs of this transform are treated as nodes */
		public Transform root;
		
		/** If no #root is set, all nodes with the tag is used as nodes */
		public string searchTag;
		
		/** Max distance for a connection to be valid. 0 = infinity */
		public float maxDistance = 0;
		
		/** Max distance along the axis for a connection to be valid. 0 = infinity */
		public Vector3 limits;
		
		/** Use raycasts to check connections */
		public bool raycast = true;
		/** Use thick raycast */
		public bool thickRaycast = false;
		/** Thick raycast radius */
		public float thickRaycastRadius = 1;
		
		public bool recursive = true;
		
		/** Layer mask to use for raycast */
		public LayerMask mask;
		
		public int CountChildren (Transform tr) {
			int c = 0;
			foreach (Transform child in tr) {
				c++;
				c+= CountChildren (child);
			}
			return c;
		}
		
		public void AddChildren (ref int c, Transform tr) {
			foreach (Transform child in tr) {
				nodes[c].position = child.position;
				nodes[c].walkable = true;
				c++;
				AddChildren (ref c,child);
			}
		}
		
		public override void Scan () {
			
			if (root == null) {
				GameObject[] gos = GameObject.FindGameObjectsWithTag (searchTag);
				
				if (gos == null) {
					CreateNodes (0);
					return;
				}
				
				nodes = CreateNodes (gos.Length);
				for (int i=0;i<gos.Length;i++) {
					nodes[i].position = gos[i].transform.position;
					nodes[i].walkable = true;
				}
			} else {
			
				if (!recursive) {
					nodes = CreateNodes (root.childCount);
				
					int c = 0;
					foreach (Transform child in root) {
						nodes[c].position = child.position;
						nodes[c].walkable = true;
						c++;
					}
				} else {
					nodes = CreateNodes (CountChildren (root));
					
					int startID = 0;
					AddChildren (ref startID,root);
				}
			}
			
			List<Node> connections = new List<Node>(3);
			List<int> costs = new List<int>(3);
			
			for (int i=0;i<nodes.Length;i++) {
				
				connections.Clear ();
				costs.Clear ();
				
				Node node = nodes[i];
				
				
				for (int j=0;j<nodes.Length;j++) {
					if (i == j) continue;
						
					Node other = nodes[j];
					
					float dist = 0;
					if (IsValidConnection (node,other,out dist)) {
						connections.Add (other);
						costs.Add (Mathf.RoundToInt (dist*Int3.FloatPrecision));
					}
				}
				
				node.connections = connections.ToArray ();
				node.connectionCosts = costs.ToArray ();
			}
		}
		
		/** Returns if the connection between \a a and \a b is valid.
		 * Checks for obstructions using raycasts (if enabled) and checks for height differences.\n
		 * As a bonus, it outputs the distance between the nodes too if the connection is valid */
		public bool IsValidConnection (Node a, Node b, out float dist) {
			dist = 0;
			
			if (!a.walkable || !b.walkable) return false;
			
			Vector3 dir = a.position-b.position;
			
			if (
				(!Mathf.Approximately (limits.x,0) && Mathf.Abs (dir.x) > limits.x) ||
				(!Mathf.Approximately (limits.y,0) && Mathf.Abs (dir.y) > limits.y) ||
				(!Mathf.Approximately (limits.z,0) && Mathf.Abs (dir.z) > limits.z))
			{
				return false;
			}
			
			dist = dir.magnitude;
			if (maxDistance == 0 || dist < maxDistance) {
				
				if (raycast) {
					
					Ray ray = new Ray (a.position,b.position-a.position);
					Ray invertRay = new Ray (b.position,a.position-b.position);
					
					if (thickRaycast) {
						if (!Physics.SphereCast (ray,thickRaycastRadius,dist,mask) && !Physics.SphereCast (invertRay,thickRaycastRadius,dist,mask)) {
							return true;
						}
					} else {
						if (!Physics.Raycast (ray,dist,mask) && !Physics.Raycast (invertRay,dist,mask)) {
							return true;
						}
					}
				} else {
					return true;
				}
			}
			return false;
		}
		
		
		public void SerializeNodes (Node[] nodes, AstarSerializer serializer) {
			//NavMeshGraph.SerializeMeshNodes (this as INavmesh, nodes, serializer);
		}
		
		public void DeSerializeNodes (Node[] nodes, AstarSerializer serializer) {
			//NavMeshGraph.DeSerializeMeshNodes (this as INavmesh, nodes, serializer);
		}
		
		public void SerializeSettings (AstarSerializer serializer) {;
		
			serializer.AddUnityReferenceValue ("root",root);
			serializer.AddValue ("maxDistance",maxDistance);
			serializer.AddValue ("limits",limits);
			serializer.AddValue ("mask",mask.value);
			serializer.AddValue ("thickRaycast",thickRaycast);
			serializer.AddValue ("thickRaycastRadius",thickRaycastRadius);
			serializer.AddValue ("searchTag",searchTag);
			serializer.AddValue ("recursive",recursive);
			serializer.AddValue ("raycast",raycast);
		}
		
		public void DeSerializeSettings (AstarSerializer serializer) {
			
			root = (Transform)serializer.GetUnityReferenceValue ("root",typeof(Transform));
			
			maxDistance = (float)serializer.GetValue ("maxDistance",typeof(float));
			limits = (Vector3)serializer.GetValue ("limits",typeof(Vector3));
			mask.value = (int)serializer.GetValue ("mask",typeof(int));
			thickRaycast = (bool)serializer.GetValue ("thickRaycast",typeof(bool));
			thickRaycastRadius = (float)serializer.GetValue ("thickRaycastRadius",typeof(float));
			searchTag = (string)serializer.GetValue ("searchTag",typeof(string));
			recursive = (bool)serializer.GetValue ("recursive",typeof(bool));
			raycast	  = (bool)serializer.GetValue ("raycast",typeof(bool),true);
		}
	}
}