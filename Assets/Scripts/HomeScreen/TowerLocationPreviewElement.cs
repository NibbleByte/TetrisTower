using UnityEngine;
using UnityEngine.UI;

namespace TetrisTower.HomeScreen
{
	public class TowerLocationPreviewElement : MonoBehaviour
	{
		[SerializeField] private Image LocationPreview;
		[SerializeField] private GameObject HiddenIndicator;

		public void SetPreview(Sprite locationSprite, bool isHidden)
		{

			LocationPreview.sprite = locationSprite;

			if (locationSprite) {
				Color grayColor = new Color(0.3f, 0.3f, 0.3f, 1f);
				LocationPreview.color = isHidden ? grayColor : Color.white;
			} else {
				LocationPreview.color = Color.black;
			}

			HiddenIndicator.SetActive(isHidden);
		}
	}
}