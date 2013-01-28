#pragma strict

public interface Damagable{
	function Damage(d:int);
	function getHealth():int;
	function getMaxHealth():int;
}