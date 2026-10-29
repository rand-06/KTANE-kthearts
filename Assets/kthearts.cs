using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

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
	private int startingCard = 13;
	private List<List<int>> takenCards;
	private List<int> points, deltaPoints;
	private int limit = 100;
	private bool heartsBroken;
	private int dealingPlayer; // 0 - South, 1 - West, 2 - North, 3 - East.
	private List<int> dealtCards;
	private List<SpriteRenderer> dealtCardRenders;
	private float localStartingY;
	private bool ModuleSolved;
	private Coroutine currentCoroutine;
	private bool TwitchPlaysActive;
	private static int ModuleIdCounter;
	private int ModuleId;
	void log(string msg){Debug.Log($"[KTHearts #{ModuleId}] {msg}");}
	void Awake(){ModuleId = ++ModuleIdCounter;}
	void newHands()
	{
		hands = Enumerable.Range(0,52).ToList().Shuffle().ToList().Chunk(13);
		hands[0] = hands[0].OrderBy(y=>
				y % 13 + (y / 13 * -2 + 7) % 5 * 13  // this means the order is ♣, ♦, ♠, ♥.
			).ToList();
		for (int i = 0; i < moduleCards.Count; i++) moduleCards[i].sprite = i<13?cards[hands[i/13][i%13]]:back;
		moduleCards.ForEach(x=>x.gameObject.SetActive(true));
	}
	void playSound(AudioClip clip){GetComponent<KMAudio>().PlaySoundAtTransform(clip.name, transform);}
	void press(int index)
	{
		if (ModuleSolved) return;
		//log($"Pressed {getCardCode(hands[0][index])}");
		if (currentState == 0)
		{
			markCard(raise(index));
			button.GetComponent<MeshRenderer>().material.color = (raisedCards.Count == 3)? Color.black: Color.white / 6f;
			return;
		}
		if (currentState < 14)
		{
			if (!readyToInput || !allowedCards(dealtCards,hands[0]).Contains(hands[0][index])) return;
			if (currentCoroutine != null && !TwitchPlaysActive) StopCoroutine(currentCoroutine);
			currentCoroutine = StartCoroutine(continueGame(index));
		}
	}

	void markCard(int card){
		if (card == 0) return;
			if (card > 0) 
				moduleCards[--card].transform.localPosition += new Vector3(0, 0,
					Mathf.Abs(selectableCard.transform.localPosition.x) / 6f);
			else 
				moduleCards[-++card].transform.localPosition -= new Vector3(0, 0,
					Mathf.Abs(selectableCard.transform.localPosition.x) / 6f);
	}

	//very simple and dumb bot algorithm, i think i'll improve it later
	int preferredCard(List<int> thrownCards, List<int> legalCards){
		if (legalCards.Count == 1) return legalCards[0];
		if (legalCards.Contains(ghostCard))
			if (thrownCards.Count > 0 && (thrownCards[0] / 13 != ghostCard / 13 || thrownCards.Count(x => x / 13 == ghostCard / 13 && x > ghostCard) > 0)) return ghostCard;
		if (thrownCards.Count > 0) return legalCards.Where(x => x!=ghostCard).OrderByDescending(x=>x%13).First();
		else return legalCards.Where(x => x!=ghostCard).OrderBy(x=>x%13).First();
	}

	int takingPlayer(int previous, List<int> thrown) => (previous + thrown.IndexOf(thrown.Where(x => x / 13 == thrown[0] / 13).OrderBy(x => x).Last())) % 4;
	//{
	//	int ans = (previous + thrown.IndexOf(thrown.Where(x => x / 13 == thrown[0] / 13).OrderBy(x => x).Last())) % 4;
	//	log($"takingPlayer({previous}, [{thrown.Select(getCardCode).Aggregate((a,b)=>$"{a}, {b}")}]):\n" +
	//	      $"thrown.Where(x => x / 13 == thrown[0] / 13).OrderBy(x => x) = {thrown.Where(x => x / 13 == thrown[0] / 13).OrderBy(x => x).Select(getCardCode).Aggregate((a,b)=>$"{a}, {b}")}\n" +
	//	      $"Index of last: {thrown.IndexOf(thrown.Where(x => x / 13 == thrown[0] / 13).OrderBy(x => x).Last())}\n" +
	//	      $"Answer: {ans}");
	//	return ans;
	//}

	IEnumerator dealCard(int player, int thrownCard)
	{
		//if (!hands[player].Contains(thrownCard)) return -1;
		log($"Dealing {getCardCode(thrownCard)} as {texts[6+player]}.");
		if (thrownCard == ghostCard) playSound(ghostsaltCurse);
		moduleCards[13*player + hands[player].IndexOf(thrownCard)].gameObject.SetActive(false);
		dealtCards.Add(thrownCard);
		hands[player] = hands[player].Replace(thrownCard, -1);
		if (!heartsBroken && thrownCard / 13 == heartsSuit)
		{
			heartsBroken = true;
			playSound(shatter);
		}
		dealtCardRenders[player].sprite = cards[thrownCard];
		dealtCardRenders[player].transform.localPosition = new Vector3(
			dealtCardRenders[player].transform.localPosition.x,
			dealtCardRenders[(player + 3) % 4].transform.localPosition.y + 0.00003f,
			dealtCardRenders[player].transform.localPosition.z
		);
		dealtCardRenders[player].gameObject.SetActive(true);
		yield return new WaitForSeconds(0.2f);
		if (dealtCards.Count != 4) {
			dealingPlayer = (player + 1) % 4;
			yield break;
		}
		int ans = takingPlayer((dealingPlayer + 1)%4, dealtCards);
		takenCards[ans].AddRange(dealtCards);
		yield return
			$"sendtochat Dealt cards were: {dealtCards.Select(getCardCode).Aggregate((a, b) => $"{a}, {b}")}, starting from {texts[6 + (dealingPlayer + 1) % 4]}. {texts[6 + ans]} takes.";
		dealtCards.Clear();
		yield return new WaitForSeconds(0.5f);
		dealtCardRenders.ForEach(x => x.transform.localPosition = new Vector3(
			x.transform.localPosition.x,
			localStartingY,
			x.transform.localPosition.z
		));
		dealtCardRenders.ForEach(x => x.gameObject.SetActive(false));
		currentState++;
		for (int i=0; i<4; i++) textMeshes[i].text = getPlayerText(i);
		log($"{texts[6+ans]} takes.");
		dealingPlayer = ans;
	}

	List<int> allowedCards(List<int> thrown, List<int> hand){
		hand = hand.Where(x => x!=-1).ToList();
		if (thrown.Count == 0)
		{
			if (currentState == 1)
			{
				if (hand.Contains(startingCard)) return new List<int> { startingCard };
				return hand;
			}
			if (heartsBroken) return hand;
			List<int> midList = hand.Where(x => x/13 != heartsSuit && x!=ghostCard).ToList();
			if (midList.Count == 0) return hand;
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
				if (midList.Count == 0) return hand;
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
			          : amountOfHearts.ToString()) + $" - {points[player]} (+{deltaPoints[player]})";
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

	IEnumerator continueGame(int selectedCard){
		readyToInput = false;
		if (selectedCard != -1) yield return dealCard(0, hands[0][selectedCard]);
		else dealingPlayer = hands.SelectMany(x => x).IndexOf(x=> x == startingCard) / 13;
		while (dealingPlayer!=0 && currentState!=14) yield return dealCard(dealingPlayer, preferredCard(dealtCards, allowedCards(dealtCards, hands[dealingPlayer])));
		if (currentState!=14) {
			readyToInput = true;
			if (TwitchPlaysActive)
			{
				if (dealtCards.Count == 0) yield return $"sendtochat No dealt cards yet. South moves.";
				else yield return $"sendtochat Dealt cards are: {dealtCards.Select(getCardCode).Aggregate((a, b) => $"{a}, {b}")}, starting from {texts[6 + (dealingPlayer + 1) % 4]}. South moves.";
				if (allowedCards(dealtCards, hands[dealingPlayer]).Count == 1)
				{
					int deal = allowedCards(dealtCards, hands[dealingPlayer]).First();
					yield return
						$"sendtochat It seems that there's only one legal move. Dealing {getCardCode(deal)} for South.";
					yield return continueGame(hands[0].IndexOf(deal));
				}
			}
		}
		else renderState14();
	}

	void renderState14()
	{
		if (currentCoroutine != null && !TwitchPlaysActive) StopCoroutine(currentCoroutine);
		currentCoroutine = null;
		button.GetComponent<MeshRenderer>().material.color = Color.black;
		if (Enumerable.Range(0,4).Any(i => takenCards[i].Count==52)){ //sunshot
			int shooter = Enumerable.Range(0,4).First(i => takenCards[i].Count()==52);
			deltaPoints = Enumerable.Range(0,4).Select(x => x==shooter?0:52).ToList();
		}
		else if (Enumerable.Range(0,4).Any(i => takenCards[i].Count(x => x/13 == heartsSuit)==13 && takenCards[i].Contains(ghostCard))){ //moonshot
			int shooter = Enumerable.Range(0,4).First(i => takenCards[i].Count(x => x/13 == heartsSuit)==13 && takenCards[i].Contains(ghostCard));
			deltaPoints = Enumerable.Range(0,4).Select(x => x==shooter?0:26).ToList();
		}
		else{
			deltaPoints = Enumerable.Range(0,4).Select(i=>(takenCards[i].Contains(ghostCard)?13:0)+takenCards[i].Count(x=>x/13==heartsSuit)).ToList();
		}
		points = points.Select((x,i)=>x+deltaPoints[i]).ToList();
		buttonText.text = getButtonText();
		button.gameObject.SetActive(true);
		for (int i=0; i<4; i++) textMeshes[i].text = getPlayerText(i);
	}

	void newGame()
	{
		points = new List<int>{0,0,0,0};
		renderState0();
	}

	void renderState0(){
		dealingPlayer = -1;
		takenCards = new List<List<int>>{new List<int>(),new List<int>(),new List<int>(),new List<int>()};
		raisedCards = new List<int>();
		textMeshes.ForEach(x=>x.text="");
		buttonText.text = getButtonText();
		button.GetComponent<MeshRenderer>().material.color = Color.white / 6f;
		dealtCards = new List<int>();
		newHands();
		deltaPoints = new List<int>{0,0,0,0};
		heartsBroken = false;
		readyToInput = false;
		button.gameObject.SetActive(currentRound % 4 < 3);
		if (currentRound % 4 == 3){
			currentState++;
			for (int i=0; i<4; i++) textMeshes[i].text = getPlayerText(i);
			if (currentCoroutine != null && !TwitchPlaysActive) StopCoroutine(currentCoroutine);
			currentCoroutine = StartCoroutine(continueGame(-1));
		}
	}

	IEnumerator buttonPress()
	{
		if (ModuleSolved) yield break;
		log("Button pressed");
		if (currentState == 0){
			if (raisedCards.Count != 3) yield break;
			button.gameObject.SetActive(false);
			currentState++;
			for (int i=0; i<4; i++) textMeshes[i].text = getPlayerText(i);
			yield return raiseAndPass();
			yield return continueGame(-1);
			yield break;
		}
		if (currentState == 14){
			currentState = 0;
			if (points.Count(x=>x>=limit)==0){
				currentRound++;
				renderState0();
			}
			else if (points[0]>=limit){
				currentRound = 0;
				newGame();
			}
			else
			{
				ModuleSolved = true;
				GetComponent<KMBombModule>().HandlePass();
				button.GetComponent<MeshRenderer>().material.color = Color.green / 2f;
				buttonText.text = "Solved!";
			}
			yield break;
		}
	}

	IEnumerator raiseAndPass(){
		raisedCards.Select(x => hands[0].IndexOf(x)).ToList().ForEach(x=> markCard(-x-1));
		List<List<int>> raisedCardsAll = new List<List<int>>{raisedCards};
		raisedCardsAll.AddRange(hands.Skip(1).Select(y => y.Shuffle().OrderByDescending(x => x % 13).Take(3).ToList()));
		List<List<int>> newRaisedCards = new List<List<int>>{new List<int>(),new List<int>(),new List<int>(),new List<int>()};
		for (int i=0; i<4; i++){
			newRaisedCards[i] = raisedCardsAll[rotationTable[currentRound%4*4+i]].ToList();
			hands[i] = hands[i].Remove(raisedCardsAll[i]).Append(newRaisedCards[i]);
		}
		hands[0] = hands[0].OrderBy(y=> y % 13 + (y / 13 * -2 + 7) % 5 * 13).ToList();
		for (int i = 0; i < moduleCards.Count; i++) moduleCards[i].sprite = i<13?cards[hands[i/13][i%13]]:back;
		raisedCards = newRaisedCards[0].Select(x=>hands[0].IndexOf(x)).ToList();
		raisedCards.ForEach(x=> markCard(x+1));
		yield return new WaitForSeconds(1f);
		raisedCards.ForEach(x=> markCard(-x-1));
	}

	void init()
	{
		button.OnInteract += delegate
		{
			currentCoroutine = StartCoroutine(buttonPress());
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
		dealtCardRenders = new List<SpriteRenderer>();
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
		dealtCardRenders.Add(nonselectableCard);
		localStartingY = nonselectableCard.transform.localPosition.y;
		for (int i=1; i<4; i++){
			SpriteRenderer newCard = Instantiate(nonselectableCard,cardParent);
			dealtCardRenders.Add(newCard);
		}
		dealtCardRenders[1].transform.localPosition = new Vector3(
			nonselectableCard.transform.localPosition.z,
			nonselectableCard.transform.localPosition.y,
			-nonselectableCard.transform.localPosition.x
		);
		dealtCardRenders[2].transform.localPosition = new Vector3(
			-nonselectableCard.transform.localPosition.x,
			nonselectableCard.transform.localPosition.y,
			-nonselectableCard.transform.localPosition.z
		);
		dealtCardRenders[3].transform.localPosition = new Vector3(
			-nonselectableCard.transform.localPosition.z,
			nonselectableCard.transform.localPosition.y,
			nonselectableCard.transform.localPosition.x
		);
		dealtCardRenders.ForEach(x=>x.gameObject.SetActive(false));
	}
	
	void Start ()
	{
		init();
		newGame();
	}

	private string TwitchHelpMessage =
		"Use !{0} pass <card> <card> <card> to pass three cards. Use !{0} continue to press the button after emptying hand. " +
		"Use !{0} <card> to deal a card from your hand. Examples of cards: 8s, h9, DK, Qc, 0h, c10, H=, td, 1s. (0, 1, 10, T or = means ten of any suit)" +
		"Use !{0} hand to print out your current hand.";

	private IEnumerator ProcessTwitchCommand(string command)
	{
		yield return null;
		command = command.ToLowerInvariant().Trim();
		if (currentState == 14)
		{
			if (command == "continue")
			{
				yield return "solve";
				yield return buttonPress();
			}
			else yield return "sendtochaterror Invalid command.";
			yield break;
		}

		if (command == "hand")
		{
			yield return $"sendtochat KTHearts: Your hand is: {hands[0].Where(x => x!=-1).Select(getCardCode).Aggregate((a,b)=>$"{a}, {b}")}.";
			yield break;
		}
		if (currentState == 0)
		{
			List<string> parameters = command.Split(' ').Where(x => x != "").ToList();
			if (parameters.Count != 4 || parameters[0] != "pass" || parameters.Skip(1).Any(x => !Regex.IsMatch(x,"^([\\djqkta=]|10)[schd]|[schd]([\\djqkta=]|10)$"))) 
			{
				yield return "sendtochaterror Invalid command.";
				yield break;
			}
			List<int> cardsToDealTP = new List<int>(){0,0,0};
			string value, suit;
			for (int i = 1; i<4; i++){
				if (Regex.IsMatch(parameters[i],"^([\\djqkta=]|10)[schd]$")){
					value = Regex.Match(parameters[i],"^([\\djqkta=]|10)([schd])$").Groups[1].Value;
					suit = Regex.Match(parameters[i],"^([\\djqkta=]|10)([schd])$").Groups[2].Value;
				}
				else if (Regex.IsMatch(parameters[i],"^[schd]([\\djqkta=]|10)$")){
					value = Regex.Match(parameters[i],"^([schd])([\\djqkta=]|10)$").Groups[2].Value;
					suit = Regex.Match(parameters[i],"^([schd])([\\djqkta=]|10)$").Groups[1].Value;
				}
				else{
					yield return "sendtochaterror Invalid command.";
					yield break;
				}
				switch (suit){
					case "s": cardsToDealTP[i-1] += 0; break;
					case "c": cardsToDealTP[i-1] += 13; break;
					case "h": cardsToDealTP[i-1] += 26; break;
					case "d": cardsToDealTP[i-1] += 39; break;
					default: yield return "sendtochaterror Invalid command."; yield break;
				}
				switch (value){
					case "2": cardsToDealTP[i-1]+=0; break;
					case "3": cardsToDealTP[i-1]+=1; break;
					case "4": cardsToDealTP[i-1]+=2; break;
					case "5": cardsToDealTP[i-1]+=3; break;
					case "6": cardsToDealTP[i-1]+=4; break;
					case "7": cardsToDealTP[i-1]+=5; break;
					case "8": cardsToDealTP[i-1]+=6; break;
					case "9": cardsToDealTP[i-1]+=7; break;
					case "j": cardsToDealTP[i-1]+=9; break;
					case "q": cardsToDealTP[i-1]+=10; break;
					case "k": cardsToDealTP[i-1]+=11; break;
					case "a": cardsToDealTP[i-1]+=12; break;
					case "1":
					case "0":
					case "10":
					case "t":
					case "=":
						 cardsToDealTP[i-1]+=8; break;
					default: yield return "sendtochaterror Invalid command."; yield break;
				}
				if (!hands[0].Contains(cardsToDealTP[i-1])){
					yield return $"sendtochaterror You dont have {getCardCode(cardsToDealTP[i-1])} in your hand."; yield break;
				}
			}
			foreach (int x in raisedCards) markCard(-hands[0].IndexOf(x)-1);
			raisedCards.Clear();
			foreach (int x in cardsToDealTP) press(hands[0].IndexOf(x));
			yield return buttonPress();
			//yield return "sendtochat Test";
			yield break;
		}

		if (currentState != 0)
		{
			if (!Regex.IsMatch(command,"^([\\djqkta=]|10)[schd]|[schd]([\\djqkta=]|10)$")) 
			{
				yield return "sendtochaterror Invalid command.";
				yield break;
			}

			int cardToDeal = 0;
			string value, suit;
				if (Regex.IsMatch(command,"^([\\djqkta=]|10)[schd]$")){
					value = Regex.Match(command,"^([\\djqkta=]|10)([schd])$").Groups[1].Value;
					suit = Regex.Match(command,"^([\\djqkta=]|10)([schd])$").Groups[2].Value;
				}
				else if (Regex.IsMatch(command,"^[schd]([\\djqkta=]|10)$")){
					value = Regex.Match(command,"^([schd])([\\djqkta=]|10)$").Groups[2].Value;
					suit = Regex.Match(command,"^([schd])([\\djqkta=]|10)$").Groups[1].Value;
				}
				else{
					yield return "sendtochaterror Invalid command.";
					yield break;
				}
				switch (suit){
					case "s": cardToDeal += 0; break;
					case "c": cardToDeal += 13; break;
					case "h": cardToDeal += 26; break;
					case "d": cardToDeal += 39; break;
					default: yield return "sendtochaterror Invalid command."; yield break;
				}
				switch (value){
					case "2": cardToDeal+=0; break;
					case "3": cardToDeal+=1; break;
					case "4": cardToDeal+=2; break;
					case "5": cardToDeal+=3; break;
					case "6": cardToDeal+=4; break;
					case "7": cardToDeal+=5; break;
					case "8": cardToDeal+=6; break;
					case "9": cardToDeal+=7; break;
					case "j": cardToDeal+=9; break;
					case "q": cardToDeal+=10; break;
					case "k": cardToDeal+=11; break;
					case "a": cardToDeal+=12; break;
					case "1":
					case "0":
					case "10":
					case "t":
					case "=":
						 cardToDeal+=8; break;
					default: yield return "sendtochaterror Invalid command."; yield break;
				}
				if (!hands[0].Contains(cardToDeal)){
					yield return $"sendtochaterror You dont have {getCardCode(cardToDeal)} in your hand."; yield break;
				}
				press(hands[0].IndexOf(cardToDeal));
		}
	}
}
