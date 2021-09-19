using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.TowerLevels.UI
{
	public class TowerNextShapePreviewIcon : MonoBehaviour
	{
		public Image Icon;

		private Image[] m_AllImages;

		private void Awake()
		{
			m_AllImages = GetComponentsInChildren<Image>();
		}

		public void SetIcon(Sprite sprite)
		{
			Icon.sprite = sprite;

			foreach(var image in m_AllImages) {
				image.enabled = sprite != null;
			}
		}
	}
}