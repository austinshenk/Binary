#pragma strict

public class Stop extends Action{
	function End(){
		var actions = GetComponent(RTSObject).actions;
		var hiddenActions = GetComponent(RTSObject).hiddenActions;
		var act:Action;
		for(var i:int = 0;i<actions.length;i++){
			act = GetComponent(typeof(actions[i])) as Action;
			act.End();
		}
		for(i = 0;i<hiddenActions.length;i++){
			act = GetComponent(typeof(hiddenActions[i])) as Action;
			act.End();
		}
	}
}