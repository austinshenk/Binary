#pragma strict

public class StationaryAttackableDamagable extends StationaryAttackable implements Damagable{
	public var health:int;
	public var healthMax:int;
	function Damage(d:int){
		if(health-d <= 0)
			Kill();
		else
			health -= d;
	}
	function getHealth():int{return health;}
	function getMaxHealth():int{return healthMax;}
}