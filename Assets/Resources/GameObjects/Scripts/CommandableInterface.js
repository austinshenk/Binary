#pragma strict

public interface Commandable{
	function addCommand(hit:RaycastHit, multi:boolean);
	function setCommand(hit:RaycastHit);
}