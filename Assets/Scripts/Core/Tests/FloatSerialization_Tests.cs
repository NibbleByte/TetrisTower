using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

namespace TetrisTower.Logic
{
	/// <summary>
	/// Test serialization of floating points.
	/// </summary>
	public class FloatSerialization_Tests
	{
		#region JSON serialization

		private void JSONTestFloats(float value)
		{
			string serialized = JsonConvert.SerializeObject(value);
			float after = JsonConvert.DeserializeObject<float>(serialized);

			Assert.AreEqual(value, after);
		}

		private void JSONTestProceduralFloats(float seed, int steps)
		{
			for(int i = 0; i < steps; i++) {
				string serialized = JsonConvert.SerializeObject(seed);
				float after = JsonConvert.DeserializeObject<float>(serialized);

				Assert.AreEqual(seed, after);

				seed += steps % 2 == 0 ? Mathf.Sqrt(seed) : seed / 5.3f;
				seed += 1 / seed;
			}

		}

		[Test]
		public void JSON_WholeNumbers()
		{
			JSONTestFloats(15);
			JSONTestFloats(15f);
			JSONTestFloats(15.0f);

			JSONTestFloats(150);
			JSONTestFloats(150f);
			JSONTestFloats(150.0f);

			JSONTestFloats(1500);
			JSONTestFloats(1500f);
			JSONTestFloats(1500.0f);

			JSONTestFloats(15000);
			JSONTestFloats(15000f);
			JSONTestFloats(15000.0f);

			JSONTestFloats(150000);
			JSONTestFloats(150000f);
			JSONTestFloats(150000.0f);

			JSONTestFloats(1500000);
			JSONTestFloats(1500000f);
			JSONTestFloats(1500000.0f);
		}

		[Test]
		public void JSON_Point_1_Numbers()
		{
			JSONTestFloats(15.1f);

			JSONTestFloats(150.1f);

			JSONTestFloats(1500.1f);

			JSONTestFloats(15000.1f);

			JSONTestFloats(150000.1f);

			JSONTestFloats(1500000.1f);
		}

		[Test]
		public void JSON_Point_2_Numbers()
		{
			JSONTestFloats(15.2f);

			JSONTestFloats(150.2f);

			JSONTestFloats(1500.2f);

			JSONTestFloats(15000.2f);

			JSONTestFloats(150000.2f);

			JSONTestFloats(1500000.2f);
		}

		[Test]
		public void JSON_Point_3_Numbers()
		{
			JSONTestFloats(15.3f);

			JSONTestFloats(150.3f);

			JSONTestFloats(1500.3f);

			JSONTestFloats(15000.3f);

			JSONTestFloats(150000.3f);

			JSONTestFloats(1500000.3f);
		}

		[Test]
		public void JSON_Point_4_Numbers()
		{
			JSONTestFloats(15.4f);

			JSONTestFloats(150.4f);

			JSONTestFloats(1500.4f);

			JSONTestFloats(15000.4f);

			JSONTestFloats(150000.4f);

			JSONTestFloats(1500000.4f);
		}

		[Test]
		public void JSON_Point_5_Numbers()
		{
			JSONTestFloats(15.5f);

			JSONTestFloats(150.5f);

			JSONTestFloats(1500.5f);

			JSONTestFloats(15000.5f);

			JSONTestFloats(150000.5f);

			JSONTestFloats(1500000.5f);
		}

		[Test]
		public void JSON_Bad_Numbers()
		{
			JSONTestFloats(0.9999999f);

			JSONTestFloats(1000000.123f);

			JSONTestFloats(134.45E-2f);
			JSONTestFloats(134.45E-10f);

			JSONTestFloats(5.555f);
			JSONTestFloats(4.333f);
			JSONTestFloats(5.555f + 4.333f);

			JSONTestFloats(5.555f / 34.417f);
		}

		[Test]
		public void JSON_Procedural_Numbers()
		{
			JSONTestProceduralFloats(0.9999999f, 1000);
			JSONTestProceduralFloats(1.43213f, 1000);
			JSONTestProceduralFloats(1.9562311351f, 1000);
			JSONTestProceduralFloats(-1000.2314135f, 1000);
			JSONTestProceduralFloats(100.43213f, 1000);
			JSONTestProceduralFloats(10000.43213f, 1000);
			JSONTestProceduralFloats(1000000.43213f, 1000);

			JSONTestProceduralFloats(324462.43213f, 1000);
		}

		#endregion

	}
}