using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using static heartext;

public class kthearts : MonoBehaviour
{

	public List<Sprite> cards;
	public Sprite back;
	public SpriteRenderer selectableCard;
	public SpriteRenderer nonselectableCard;
	public Transform cardParent;
	public KMSelectable button;
	public TextMesh buttonText;
	public List<TextMesh> textMeshes;
	public AudioClip shatter, ghostsaltCurse;
	public KMAudio Audio;

	private readonly List<string> cardCodes = new List<string>
	{
		"2s", "3s", "4s", "5s", "6s", "7s", "8s", "9s", "Ts", "Js", "Qs", "Ks", "As",
		"2c", "3c", "4c", "5c", "6c", "7c", "8c", "9c", "Tc", "Jc", "Qc", "Kc", "Ac",
		"2h", "3h", "4h", "5h", "6h", "7h", "8h", "9h", "Th", "Jh", "Qh", "Kh", "Ah",
		"2d", "3d", "4d", "5d", "6d", "7d", "8d", "9d", "Td", "Jd", "Qd", "Kd", "Ad"
	};

	private string getCardCode(int index) => $"{"23456789TJQKA"[index % 13]}{"schd"[index / 13]}";
	
	private List<int> raisedCards = new List<int>();

	private List<List<int>> hands;
	private List<SpriteRenderer> moduleCards = new List<SpriteRenderer>();
	private bool readyToInput;
	private int currentState; // 0 - passing cards, 1-13 - dealing cards, 14 - results screen.
	private int currentRound;
	private readonly List<int> rotationTable = new List<int>{3,0,1,2,1,2,3,0,2,3,0,1};
	private List<string> texts = new List<string>{
		"Pass left","Pass right", "Pass across", "Continue", "Restart", "Solve", "South", "West", "North", "East", "Q", "Moon", "Sun"
	};
	private int ghostCard = 10;
	private int heartsSuit = 2;
	private int startingCard = 0;
	private List<List<int>> takenCards;
	private List<int> points, deltaPoints;
	private int limit = 100;
	private bool heartsBroken;
	private int dealingPlayer = -1; // 0 - South, 1 - West, 2 - North, 3 - East.
	private List<int> dealtCards;
	

	void newHands()
	{
		hands = Enumerable.Range(0,52).ToList().Shuffle().ToList().Chunk(13).Select(x=>x.OrderBy(y=>
				y % 13 + (y / 13 * -2 + 7) % 5 * 13  // this means the order is ♣, ♦, ♠, ♥.
			).ToList()).ToList();
		for (int i = 0; i < moduleCards.Count; i++) moduleCards[i].sprite = i<13?cards[hands[0][i]]:back;
		dealingPlayer = hands.SelectMany(x => x).IndexOf(x=> x == startingCard) / 13;
		deltaPoints = new List<int>{0,0,0,0};
		heartsBroken = false;
	}
	void playSound(AudioClip clip){Audio.PlaySoundAtTransform(clip.name, transform);}
	void press(int index)
	{
		print($"Pressed {getCardCode(hands[0][index])}");
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

			return;
		}

		if (currentState < 14)
		{
			
		}
	}
	
	int takingPlayer(int previous, List<int> thrown) => (previous + thrown.IndexOf(thrown.Where(x=>x/13 == thrown[0]/13).OrderBy(x=>x).Last()))%4;

	int dealCard(int player, int thrownCard)
	{
		if (!hands[player].Contains(thrownCard)) return -1;
		if (thrownCard == ghostCard) playSound(ghostsaltCurse);
		hands[player].Remove(thrownCard);
		dealtCards.Add(thrownCard);
		if (!heartsBroken && thrownCard / 13 == heartsSuit)
		{
			heartsBroken = true;
			playSound(shatter);
		}
		if (dealtCards.Count != 4) return (player + 1) % 4;
		int ans = takingPlayer(dealingPlayer, dealtCards);
		takenCards[ans].AddRange(dealtCards);
		dealtCards.Clear();
		return ans;
	}

	List<int> allowedCards(List<int> thrown, List<int> hand)
	{
		if (thrown.Count == 0)
		{
			if (heartsBroken) return hand.ToList();
			List<int> midList = hand.Where(x => !(x/13 == heartsSuit || x!=ghostCard)).ToList();
			if (midList.Count == 0) return hand.ToList();
			return midList;
		}
		else
		{
			int startingSuit = thrown[0] / 13;
			if (currentState == 1)
			{
				List<int> midList = hand.Where(x => x/13 == startingSuit && x!=ghostCard && x/13!=heartsSuit).ToList();
				if (midList.Count != 0) return midList;
				midList = hand.Where(x => x!=ghostCard && x/13!=heartsSuit).ToList();
				if (midList.Count != 0) return midList;
				midList = hand.Where(x => x/13!=heartsSuit).ToList();
				if (midList.Count != 0) return midList;
				return hand;
			}
			else
			{
				List<int> midList = hand.Where(x => x/13 == startingSuit).ToList();
				if (midList.Count == 0) return hand.ToList();
				return midList;
			}
		}
	}

	string getPlayerText(int player){
		if (currentState == 0) return "";
		bool hasGhost = takenCards[player].Contains(ghostCard);
		int amountOfHearts = takenCards[player].Count(x => x/13 == heartsSuit);
		if (currentState < 14) return texts[6+player] + ": " + (hasGhost
			? (amountOfHearts == 13 ? texts[11] : texts[10] + "+" + amountOfHearts)
			: amountOfHearts.ToString());
		if (takenCards[player].Count == 52) return $"{texts[6+player]}: {texts[12]} - {points[player]} (+{deltaPoints[player]})";
		return texts[6+player] + ": " + (hasGhost
			          ? (amountOfHearts == 13 ? texts[11] : texts[10] + "+" + amountOfHearts)
			          : amountOfHearts.ToString()) + $" - {points[player]} (+{deltaPoints[player]})\"";
	}

	string getButtonText(){
		switch (currentState)
		{
			case 0:
				return currentRound % 4 == 3? "":texts[currentRound%4];
			case 14:
				return points.Count(x => x >= limit) == 0 ? texts[3] : points[0] >= limit ? texts[4] : texts[5];
			default:
				return "";
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

	void startGame(){
		currentState++;
	}

	void newGame()
	{
		points = new List<int>{0,0,0,0};
		takenCards = new List<List<int>>{new List<int>(),new List<int>(),new List<int>(),new List<int>()};
		textMeshes.ForEach(x=>x.text="");
		button.GetComponent<MeshRenderer>().material.color = Color.white / 6f;
		newHands();
		button.gameObject.SetActive(currentRound % 4 < 3);
		if (currentRound % 4 == 3){
			startGame();
		}
	}

	void buttonPress()
	{
		print("Button pressed");
		if (currentState == 0){
			if (raisedCards.Count != 3) return;
			raiseAndPass();
			button.gameObject.SetActive(false);
		}
	}

	void raiseAndPass(){
		List<List<int>> raisedCardsAll = new List<List<int>>{raisedCards};
		raisedCardsAll.AddRange(hands.Skip(1).Select(y => y.OrderBy(x => x / 13 * 13 + 12 - x % 13).Take(3).ToList()));
		List<List<int>> newRaisedCards = new List<List<int>>{new List<int>(),new List<int>(),new List<int>(),new List<int>()};
		for (int i=0; i<4; i++){
			newRaisedCards[i] = raisedCardsAll[rotationTable[currentRound%4*4+i]];
			hands[i] = hands[i].Remove(raisedCardsAll[i]).Append(newRaisedCards[i]);
		}

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
		init();
		newGame();
	}
}
