#pragma strict

public interface Commandable{
	function addCommand(hit:RaycastHit);
	function setCommand(hit:RaycastHit);
}