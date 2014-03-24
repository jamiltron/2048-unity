using UnityEngine;
using System.Collections;

public class TileAnimationHandler : MonoBehaviour {

	public float scaleSpeed;

	private Transform _transform;

	public void AnimateEntry() {
		StartCoroutine("AnimationEntry");
	}

	private IEnumerator AnimationEntry() {
		while (_transform == null) yield return null;

		_transform.localScale = new Vector3(0.25f, 0.25f, 1f);
		while (_transform.localScale.x < 1f) {
			_transform.localScale = Vector3.MoveTowards(_transform.localScale, Vector3.one, scaleSpeed * Time.deltaTime);
			yield return null;
		}
	}

	// Use this for initialization
	void Start () {
		_transform = transform;
	}
}
