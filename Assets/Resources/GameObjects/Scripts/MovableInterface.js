#pragma strict

public interface Movable{
	function Move(point:Vector3, interruptable:boolean);
	function setCommandPointOffset(point:Vector3);
	function setFlocking(f:boolean);
	function Ended():boolean;
}