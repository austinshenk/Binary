using UnityEngine;
using System.Collections;
using UnityEditor;

//This is a small class for holding undo state objects. The graph editors will serialize graphs from time to time and put the data in an instance of the GraphUndo class with the "hasBeenReverted" flag set to true, and then push it to Unity's undo system. Then it will set "hasBeenReverted" to false again. It will always listen for if the stored instance of a GraphUndo object has the hasBeenReverted flag set to true, if it is, it means that the user has undone something and then we can deserialize the data.
public class GraphUndo : ScriptableObject {
	
	public bool hasBeenReverted = false;
	
	public byte[] data;
}