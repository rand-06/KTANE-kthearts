using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using static HEARTEXT;

public class KTHEARTS : MonoBehaviour
{

	public List<Sprite> cards;
	public Sprite back;
	public SpriteRenderer selectableCard;
	public SpriteRenderer nonselectableCard;
	public Transform cardParent;
	public KMSelectable button;
	public List<TextMesh> texts;
	public AudioClip shatter, ghostsaltCurse;
	public KMAudio Audio;

	private readonly List<string> cardCodes = new List<string>
	{
		"2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s", "=s", "Js", "Qs", "Ks", "As",
		"2c", "3c", "4c", "5c", "6c", "7c", "8c", "9c", "=c", "Jc", "Qc", "Kc", "Ac",
		"2h", "3h", "4h", "5h", "6h", "7h", "8h", "9h", "=h", "Jh", "Qh", "Kh", "Ah",
		"2d", "3d", "4d", "5d", "6d", "7d", "8d", "9d", "=d", "Jd", "Qd", "Kd", "Ad"
	};
	
	private List<int> raisedCards = new List<int>();

	private List<List<int>> hands;
	private List<SpriteRenderer> moduleCards = new List<SpriteRenderer>();
	private int startingHand = -1; // 0 - South, 1 - West, 2 - North, 3 - East.
	private bool readyToInput;
	private int currentState; // 0 - passing cards, 1-13 - dealing cards, 14 - results screen.

	void newHands()
	{
		hands = Enumerable.Range(0,52).ToList().Shuffle().ToList().Chunk(13).Select(x=>x.OrderBy(y=>
				y % 13 + (y / 13 * -2 + 7) % 5 * 13  // this means the order is ♣, ♦, ♠, ♥.
			).ToList()).ToList();
		for (int i = 0; i < moduleCards.Count; i++) moduleCards[i].sprite = i<13?cards[hands[0][i]]:back;
		startingHand = hands.SelectMany(x => x).IndexOf(x=> x == 0) / 13;
	}
	void playSound(AudioClip clip){Audio.PlaySoundAtTransform(clip.name, transform);}
	void press(int index)
	{
		print($"Pressed {cardCodes[hands[0][index]]}");
		if (currentState == 0)
		{
			int card = raise(index);
			if (card == 0) return;
			if (card > 0) 
				moduleCards[--card].transform.localPosition += new Vector3(0, 0,
					Mathf.Abs(nonselectableCard.transform.localPosition.x) / 6f);
			else 
				moduleCards[-++card].transform.localPosition -= new Vector3(0, 0,
					Mathf.Abs(nonselectableCard.transform.localPosition.x) / 6f);
			button.GetComponent<MeshRenderer>().material.color = (raisedCards.Count == 3)? Color.black: Color.white / 6f;
		}
	}

	int raise(int index)
	{
		int card = hands[0][index];
		if (raisedCards.Contains(card))
		{
			raisedCards.Remove(card);
			return -index-1;
		}
		if (raisedCards.Count > 2) return 0;
		raisedCards.Add(card);
		return index+1;
	}

	void newGame()
	{
		texts.ForEach(x=>x.text="");
		button.GetComponent<MeshRenderer>().material.color = Color.white / 6f;
		newHands();
	}

	void buttonPress()
	{
		print("Button pressed");
	}

	void init()
	{
		button.OnInteract += delegate
		{
			buttonPress();
			return false;
		};
		moduleCards.Add(selectableCard);
		for (int i = 1; i < 13; i++)
		//int i = 1;
		{
			SpriteRenderer newCard = Instantiate(selectableCard, cardParent);
			newCard.transform.localPosition = new Vector3(
				selectableCard.transform.localPosition.x + Mathf.Abs(2 * selectableCard.transform.localPosition.x) * i / 12f, 
				selectableCard.transform.localPosition.y + 0.00003f * i, 
				selectableCard.transform.localPosition.z);
			newCard.sortingOrder = i;
			moduleCards.Add(newCard);
			int i1 = i;
			newCard.GetComponentInChildren<KMSelectable>().OnInteract += delegate
			{
				press(i1);
				return false;
			};
		}
		selectableCard.GetComponentInChildren<KMSelectable>().OnInteract += delegate
		{
			press(0);
			return false;
		};
		GetComponent<KMSelectable>().Children = moduleCards.Select(x=>x.GetComponentInChildren<KMSelectable>()).ToList().Append(button).ToArray();
		GetComponent<KMSelectable>().UpdateChildrenProperly();
		
		for (int i = 0; i < 13; i++)
		{
			SpriteRenderer newCard = Instantiate(nonselectableCard, cardParent);
			newCard.transform.localRotation = Quaternion.Euler(
					nonselectableCard.transform.localRotation.eulerAngles.x,
					nonselectableCard.transform.localRotation.eulerAngles.y,
					nonselectableCard.transform.localRotation.eulerAngles.z - 90f
				);
			newCard.sortingOrder = i;
			moduleCards.Add(newCard);
			newCard.transform.localPosition = new Vector3(
				selectableCard.transform.localPosition.z,
				nonselectableCard.transform.localPosition.y + 0.00003f * i,
				-selectableCard.transform.localPosition.x - Mathf.Abs(2 * selectableCard.transform.localPosition.x) * i / 12f
			);
		}
		for (int i = 0; i < 13; i++)
		{
			SpriteRenderer newCard = Instantiate(nonselectableCard, cardParent);
			newCard.transform.localRotation = Quaternion.Euler(
				nonselectableCard.transform.localRotation.eulerAngles.x,
				nonselectableCard.transform.localRotation.eulerAngles.y,
				nonselectableCard.transform.localRotation.eulerAngles.z - 180f
			);
			newCard.sortingOrder = i;
			moduleCards.Add(newCard);
			newCard.transform.localPosition = new Vector3(
				-selectableCard.transform.localPosition.x - Mathf.Abs(2 * selectableCard.transform.localPosition.x) * i / 12f,
				nonselectableCard.transform.localPosition.y + 0.00003f * i,
				-selectableCard.transform.localPosition.z 
			);
		}
		for (int i = 0; i < 13; i++)
		{
			SpriteRenderer newCard = Instantiate(nonselectableCard, cardParent);
			newCard.transform.localRotation = Quaternion.Euler(
				nonselectableCard.transform.localRotation.eulerAngles.x,
				nonselectableCard.transform.localRotation.eulerAngles.y,
				nonselectableCard.transform.localRotation.eulerAngles.z - 270f
			);
			newCard.sortingOrder = i;
			moduleCards.Add(newCard);
			newCard.transform.localPosition = new Vector3(
				-selectableCard.transform.localPosition.z ,
				nonselectableCard.transform.localPosition.y + 0.00003f * i,
				selectableCard.transform.localPosition.x + Mathf.Abs(2 * selectableCard.transform.localPosition.x) * i / 12f 
			);
		}
		
		nonselectableCard.gameObject.SetActive(false);
	}
	
	void Start ()
	{
		//selectableCard.GetComponentInChildren<KMSelectable>().OnInteract += delegate () { print("a"); return false; };
		init();
		newGame();
	}
}
