using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardBoomerangTrick : Trick
{
    public static CardBoomerangTrick s;

    [SerializeField] int numberThrowsInTrick = 10;
    int throwsLeft;
    private float maxPointsPerCard;

    [Header("Trick-Specific Scoring Variables")]
    [SerializeField] float percentPenaltyForSlowThrow =.25f;
    [SerializeField] float timeBetweenThrowsForSlowThrow =5;
    [SerializeField] float percentPenaltyForRepeatedZone =.25f;
    Collider lastZoneThrownInto;

    [Header("Combo Settings")]
    [SerializeField] private int numberCatchesToAdvanceCombo = 4;
    private int numCatchesForCombo = 0;

    [Header("Clap Score Settings")]
    [SerializeField] private int clapsPerStep;
    [SerializeField] private int[] requiredCardsInThrow;
    private int currentClapStep = 0;
    private int throwSteps;
    private bool sufficientThrowInProgress =false;
    private int catchesInCurrentThrow = 0;
    private List<cardCaughtData> currentThrowData;
    private Dictionary<int, cardCaughtData> currentThrowDict;
    private int throwTriesLeft;

    //variables for tracking slow throws
    float timeLastThrow = -1;

    private PropUIManager myUIMan;

    //Variables for analytics
    private int numberCardsCaught =0;
    private int numberSlowThrows = 0;
    private int numberDuplicateThrows = 0;

    //Checklist Stuff
    public static bool cardCaught = false;

    // Start is called before the first frame update
    void Start()
    {
        if (s != null)
        {
            Debug.LogWarning("Mustliple CardBoomerangTricks are in this scene, you need to remove one.");
        }
        else s = this;

        maxPointsPerCard = maxScore / numberThrowsInTrick;
        throwsLeft = numberThrowsInTrick;

        currentThrowData = new List<cardCaughtData>();
        currentThrowDict = new Dictionary<int, cardCaughtData>();

        cardCaught = false;
    }

    public override void EndTrick()
    {
        //Stop the deck from respawning
        BoomerangDeck deck = FindObjectOfType<BoomerangDeck>();
        deck.GetComponent<ItemRespawn>().DisableRespawn();

        base.EndTrick();
    }

    public override void Initialize()
    {
        base.Initialize();
        myUIMan = trickPrefabHolder.propUIManager;

        if (usingClapScoring)
        {
            throwSteps = requiredCardsInThrow.Length;
            throwTriesLeft = throwSteps;
        }
    }

    public void ReportLastZoneThrownInto(Collider zone, BoomerangCard card)
    {
        //assign card zone penalty if we had a repeated zone
        if (lastZoneThrownInto != null)
        {
            if (zone == lastZoneThrownInto)
            {
                numberDuplicateThrows += 1;
                card.repeatZone = true;
            }
        }

        //assign card slow penalty if it was a slow throw
        if (CheckIfThrowWasSlow())
        {
            card.slowThrow = true;
        }

        //set variables for next time
        lastZoneThrownInto = zone;
        timeLastThrow = Time.time;
    }

    public void CheckThrowSufficiency(List<BoomerangCard> cardsInActiveThrow)
    {
        //Don't keep going if there's already a throw going on
        if (sufficientThrowInProgress) return;

        int requiredCardsForSufficiency = requiredCardsInThrow[currentClapStep];
        if (cardsInActiveThrow.Count < requiredCardsForSufficiency) return;

        //We threw enough card, log the data
        sufficientThrowInProgress = true;

        currentThrowData.Clear();
        currentThrowDict.Clear();
        foreach (BoomerangCard c in cardsInActiveThrow)
        {
            BuildAndRecordCardCaughtData(c);
        }

        //InWorldDebug.Log("New sufficient throw reged w/ card#: " + currentThrowData.Count.ToString() + "/" + currentTHrowDict.Count.ToString());
    }

    private void BuildAndRecordCardCaughtData(BoomerangCard c)
    {
        cardCaughtData ccd = new cardCaughtData();
        ccd.cardIndex = c.GetThrowIndex();
        ccd.caught = false;
        ccd.dropped = false;
        ccd.scored = false;
        currentThrowData.Add(ccd);
        currentThrowDict.Add(ccd.cardIndex, ccd);
    }

    public void ScoreCard(bool caught, bool slowThrow, bool repeatZone, BoomerangCard card)
    {
        if (!usingClapScoring)
        {
            throwsLeft--;
            if (caught)
            {
                
                numberCardsCaught += 1;
                numCatchesForCombo++;

                float points = maxPointsPerCard;

                //reduce points if slow throw
                if (slowThrow) points *= (1 - percentPenaltyForSlowThrow);

                //reduce points if repeated zone
                if (repeatZone) points *= (1 - percentPenaltyForRepeatedZone);

                //currentScore += (int)points;

                points = TrickManager.s.ScorePoints(points);

                if (numCatchesForCombo >= numberCatchesToAdvanceCombo)
                {
                    myUIMan.SpawnFlyingFeedback(points, false, comboFeedback: PropUIManager.ComboUIChangeType.COMBOUP);
                }
                else
                    myUIMan.SpawnFlyingFeedback(points, false);
            }
            //dropped
            else
            {
                //just spawn bad feedback
                myUIMan.SpawnFlyingFeedback(0, true, comboFeedback: PropUIManager.ComboUIChangeType.COMBOBUST);

                //Bust the combo
                numCatchesForCombo = 0;
                TrickManager.s.LoseCombo();
            }

            //Modify Combo If appropriate
            if (numCatchesForCombo >= numberCatchesToAdvanceCombo)
            {
                numCatchesForCombo = 0;
                TrickManager.s.AdvanceComboStep();
            }

            //end trick if that was enough throws
            if (throwsLeft <= 0)
            {
                EndTrick();
                Destroy(gameObject);
            }
        }
        //Using clap scoring
        else
        {
            if (!sufficientThrowInProgress) return;
            bool throwComplete = CheckThrowDataForCompletion(card, caught); //The actually scoring happens downstream in this function
            if (throwComplete)
            {
                cardCaught = true;
                sufficientThrowInProgress = false;
            }
        }
    }

    private bool CheckThrowDataForCompletion(BoomerangCard card, bool caught)
    {
        bool throwCompleted = false;

        //Incorporate the new data
        cardCaughtData ccd;
        //Debug.Log("aaron Index I'm trying to reference: " + card.GetThrowIndex().ToString());
        if  (currentThrowDict.TryGetValue(card.GetThrowIndex(), out ccd))
        {
            if (caught)
            {
                ccd.caught = true;
            }
            else
            {
                ccd.dropped = true;
            }
            ccd.scored = true;
        }
        else
        {
            //InWorldDebug.Log("There was a problem with the current throw dictionary in boomerang trick.");
            //We should hit here if you cantch a card that wasn't part of the current throw
            return false;
        }

        bool allScored = true;
        foreach (cardCaughtData ccd2 in currentThrowData)
        {
            //Debug.Log("aaron Index: "+ccd2.cardIndex.ToString()+"   bool: " + ccd2.scored.ToString()); //I should see a false then a true on a 2 card throw
            if (!ccd2.scored)
            {
                allScored = false;
                continue;
            }
        }

        if (allScored)
        {
            //InWorldDebug.Log("We scored.");
            throwCompleted = true;
            GiveFeedbackAndScoreAndEndTrick();
        }

        return throwCompleted;
    }

    private void GiveFeedbackAndScoreAndEndTrick()
    {
        bool successfulThrow = true;
        foreach (cardCaughtData ccd in currentThrowData)
        {
            if (ccd.dropped)
            {
                successfulThrow = false;
                continue;
            }
        }

        if (successfulThrow)
        {
            GameManager.s.ScoreClaps(clapsPerStep);
            trickBotched = false;
        }
        else
        {
            AkSoundEngine.PostEvent("audienceNegLines", gameObject);
            //give neg feedback
        }

        currentClapStep++;
        throwTriesLeft--;
        if (throwTriesLeft <= 0)
        {
            EndTrick();
            Destroy(gameObject);
        }
    }

    public bool CheckIfThrowWasSlow()
    {
        bool slowThrow = false;
        if (timeLastThrow == -1) return slowThrow; //if this is the first throw or w/e, just return false now.


        if ((Time.time - timeLastThrow) >= timeBetweenThrowsForSlowThrow)
        {
            numberSlowThrows += 1;
            slowThrow = true;
        }

        return slowThrow;
    }

    public override void CreateAnalytics()
    {
        base.CreateAnalytics();
        AnalyticsManager.s.AddToDictionary("Duplicate throw count", numberDuplicateThrows);
        AnalyticsManager.s.AddToDictionary("Slow throw count", numberSlowThrows);
        AnalyticsManager.s.AddToDictionary("Number cards caught", numberCardsCaught);
    }

    private class cardCaughtData
    {
        public int cardIndex;
        public bool caught;
        public bool dropped;
        public bool scored;
    }
}
