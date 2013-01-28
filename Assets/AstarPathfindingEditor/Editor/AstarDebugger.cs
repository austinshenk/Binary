using UnityEngine;
using System.Collections;
using System.Text;

[AddComponentMenu("Pathfinding/Debugger")]
public class AstarDebugger : MonoBehaviour {
	
	private AstarPath astar;
	
	public int yOffset = 5;
	
	public void Start () {
		astar = AstarPath.active;
	}
	
	public void OnGUI () {
		
		//StringBuilder text = new StringBuilder ();
		string text = "A* Pathfinding Project Debugger\n";
		
		
		text += "Astar Version "+AstarPath.Version.ToString ();
		if (AstarPath.pathQueueStart != null) {
			text += "\nLast Added Path ID				" + AstarPath.pathQueueEnd.pathID;
			text += "\nCurrently Computing Path ID	" + AstarPath.pathQueueStart.pathID + (AstarPath.pathQueueStart.next != null ? " (has next)":"");
			if (AstarPath.pathReturnQueueStart != null) {
				text += "\nLast Returned Path ID			" + AstarPath.pathReturnQueueStart.pathID + (AstarPath.pathReturnQueueStart.next != null ? " (has next)":"");
			}
			text += "\nMax Frame Time					" + astar.maxFrameTime+"ms";
			
			double searchSpeed = (double)AstarPath.TotalSearchedNodes*10000 / (double)AstarPath.TotalSearchTime;
			
			text += "\nSearch Speed	(nodes/ms)	" + searchSpeed.ToString ("0") + " ("+AstarPath.TotalSearchedNodes+" / "+((double)AstarPath.TotalSearchTime/10000F).ToString ("0")+")";
		
			if (AstarPath.pathReturnQueueStart != null) {
				text += "\nReturn delay							"+((System.DateTime.Now-AstarPath.pathReturnQueueStart.callTime).TotalMilliseconds.ToString ("0.0"));
			}
			text += "\nPathfinding Thread					" + (AstarPath.activeThread != null ? (AstarPath.activeThread.IsAlive ? "Alive" : "Dead") : "Null");
		}
		GUI.Box (new Rect (5,yOffset,310,140),"");
		GUI.Label (new Rect (10,yOffset,1000,200),text.ToString ());
	}
}
