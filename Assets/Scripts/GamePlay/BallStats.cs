using UnityEngine;

namespace Golf.GamePlay
{
	public enum BallEffect
	{
		Add,
		Multiply,
		Subtract
	}

	[CreateAssetMenu(fileName = "BallStats", menuName = "Game/Ball Stats")]
	public class BallStats : ScriptableObject
	{
		public BallEffect effect;
		[Min(1)] public int value = 1;
		[Range(1, 100)] public int spawnWeight = 1;
		[Min(0.1f)] public float speedMultiplier = 1f;
		public Color color = Color.white;
		public Mesh mesh;
		public bool useOpaqueTint;
	}
}
