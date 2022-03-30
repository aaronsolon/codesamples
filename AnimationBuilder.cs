/** ------------------------------------------------------------------------------
- Filename:   AnimationBuilder
- Project:    BPG-Africa 
- Developers: Michigan State University GEL Lab
- Created on: 2021/08/18
- Created by: Aaron Solon
------------------------------------------------------------------------------- */

#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "New Animation Builder",menuName = "Animation Builder")]
public class AnimationBuilder : ScriptableObject 
{
    public enum AnimationLoopType { REGULARLOOP, BACKANDFORTH}
    public enum SheetIdentity { SHEET1, SHEET2}

    public Sprite[] testImagesForAnimation;
    public AnimationLoopType loopType;
    public float FPS = 60;
    public float testTimeBetweenFrames = .05f;
    public string testAnimName;

    [Header("File path")]
    public string saveFilePath;

    [Header("Variables for building from spritesheets")]
    [SerializeField] private List <InfoToBuildAnimationsFromSheet> infoToBuildFromSheets = new List<InfoToBuildAnimationsFromSheet>();

    private string suffix = ".anim";

    /// <summary>
    /// This function is called by pressing a button once the files are assigned correctly in the inspector.
    /// </summary>
    public void BuildAnimationsFromAnimationSheets()
    {
        foreach (InfoToBuildAnimationsFromSheet info in infoToBuildFromSheets)
        {
            int[] imageCounts = new int[0];
            bool[] loopBackBools = new bool[0];
            float[] timesBetweenFrames = new float[0];
            string[] fileTitles = new string[0];

            //Assign the arrays based on which sheet we're pulling from
            switch (info.sheetIdentity)
            {
                case SheetIdentity.SHEET1:
                    imageCounts = AnimationBuilder.spriteSheet1ImageCounts;
                    loopBackBools = AnimationBuilder.spriteSheet1LoopbackDatas;
                    timesBetweenFrames = AnimationBuilder.spriteSheet1TimeBetweenFrames;
                    fileTitles = AnimationBuilder.spriteSheet1AnimationTitles;
                    break;

                case SheetIdentity.SHEET2:
                    imageCounts = AnimationBuilder.spriteSheet2ImageCounts;
                    loopBackBools = AnimationBuilder.spriteSheet2LoopbackDatas;
                    timesBetweenFrames = AnimationBuilder.spriteSheet2TimeBetweenFrames;
                    fileTitles = AnimationBuilder.spriteSheet2AnimationTitles;
                    break;
            }

            //Attach the file name prefix to the animation names
            string[] tempFileTitles = new string[fileTitles.Length];
            for (int s = 0; s < fileTitles.Length; s++)
            {
                tempFileTitles[s] = info.animationFilenamePrefix + fileTitles[s];
            }

            //Check to see if we have the right number of images, and return an error message if we don't
            int totalImageCheck = 0;
            foreach (int i in imageCounts)
            {
                totalImageCheck += i;
            }
            if (totalImageCheck != info.allSpritesFromSpriteSheet.Length)
            {
                Debug.LogWarning("The number of sprites entered into AnimationBuilder for " + info.sheetIdentity.ToString() + " does not match its expected value, animations cannot be built.");
                Debug.LogWarning("The number of images found in the imageCounts variable: " + totalImageCheck.ToString());
                Debug.LogWarning("The length of the images entered in the inspector: " + info.allSpritesFromSpriteSheet.Length.ToString());
                return;
            }

            //set up a variable to count the number of images already used
            int imagesAlreadyUsed = 0;

            //Loop through each intended animation, and build it using the data we have set up
            for (int i=0; i< imageCounts.Length; i++)
            {
                //initialize the necesarry variables
                float timeBetweenFrames = timesBetweenFrames[i];
                string animationName = tempFileTitles[i];
                bool loopBack = loopBackBools[i];
                int imageCount = imageCounts[i];
                Sprite[] imagesForAnimation = new Sprite[imageCount];

                //Assign the sprites
                int tempIndex = 0;
                int tempImagesUsed = imagesAlreadyUsed;
                for (int x = imagesAlreadyUsed; x < (tempImagesUsed + imageCount); x++)
                {
                    imagesForAnimation[tempIndex] = info.allSpritesFromSpriteSheet[x];
                    tempIndex++;
                    imagesAlreadyUsed++;
                }

                //Build the animation
                BuildAnimation(imagesForAnimation, timeBetweenFrames, animationName, saveFilePath, animationLoopsBack: loopBack);
            }
        }
    }

    public void BuildAnimation(Sprite[] imagesForAnimation,float timeBetweenFrames,string animName,string saveFilePath, bool animationLoopsBack = false)
    {
        AnimationClip aClip = new AnimationClip();
        aClip.frameRate = FPS;

        EditorCurveBinding spriteBinding = new EditorCurveBinding();
        spriteBinding.type = typeof(SpriteRenderer);
        spriteBinding.path = "";
        spriteBinding.propertyName = "m_Sprite";

        ObjectReferenceKeyframe[] spriteKeyFrames = new ObjectReferenceKeyframe[imagesForAnimation.Length];
        
        for (int i = 0; i<spriteKeyFrames.Length; i++)
        {
            spriteKeyFrames[i] = new ObjectReferenceKeyframe();
            spriteKeyFrames[i].time = i * timeBetweenFrames;
            spriteKeyFrames[i].value = imagesForAnimation[i];
        }



        //If the animation loops back on itself, assign keyframes that way
        if (animationLoopsBack)
        {
            float timeDeltaBetweenFrames = Mathf.Abs(spriteKeyFrames[0].time - spriteKeyFrames[1].time);
            int numberOfFramesToAdd = spriteKeyFrames.Length - 2;
            //add all existing frames to a temporary list
            List<ObjectReferenceKeyframe> tempFrames = new List<ObjectReferenceKeyframe>();
            foreach (ObjectReferenceKeyframe oK in spriteKeyFrames)
            {
                tempFrames.Add(oK);
            }

            for (int i = 0; i < numberOfFramesToAdd; i++)
            {
                //working backwards from the end of the frames, add frames back one by one
                tempFrames.Add(CopyKeyframe(spriteKeyFrames[spriteKeyFrames.Length - (i + 2)], spriteKeyFrames[spriteKeyFrames.Length - 1].time + ((i+1)*timeDeltaBetweenFrames)));
            }

            //slot the templist back into the keyframes
            spriteKeyFrames = new ObjectReferenceKeyframe[tempFrames.Count];

            for (int i =0; i<tempFrames.Count; i++)
            {
                spriteKeyFrames[i] = tempFrames[i];
            }
        }

        AnimationUtility.SetObjectReferenceCurve(aClip, spriteBinding, spriteKeyFrames);

        //for now, set all to loop, if an animation later on doesn't loop this will need to be expanded
        AnimationClipSettings set = AnimationUtility.GetAnimationClipSettings(aClip);
        set.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(aClip, set);

        string fileName = "/" + animName + suffix;

        string finalPath = saveFilePath + fileName;

        AssetDatabase.CreateAsset(aClip, finalPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private ObjectReferenceKeyframe CopyKeyframe(ObjectReferenceKeyframe original, float newTime)
    {
        ObjectReferenceKeyframe newKey = new ObjectReferenceKeyframe();
        newKey.time = newTime;
        newKey.value = original.value;
        return newKey;
    }
}

public class AnimationBuildData
{
    public Sprite[] imagesForAnimation;
    public string fileName;
    public float timeBetweenFrames;
    public bool animationLoopsBack;
}

public static int[] spriteSheet1ImageCounts = new int[]
    {
        5, //walk front
        5, //walk back
        5, //idle front
        5, //idle back
        6, //running front
        6, //running back
        6, //clay gather front
        6, //clay gather back
        6, //axeing a question
        6, //dancing
        6, //hammering
        6,//tilling
        5, //gathering
        2, //drumming with more detail
        2, //drumming less detail
        1 //stool
    };

    public static string[] spriteSheet1AnimationTitles = new string[]
    {
        "walk_front",
        "walk_back",
        "idle_front",
        "idle_back",
        "running_front",
        "running_back",
        "clay_gather_front",
        "clay_gather_back",
        "chopping_tree",
        "dancing",
        "repairing",
        "farming",
        "rummaging",
        "drumming_detailed",
        "drumming",
        "sitting"
    };

    //every :05 is .084 (walking, dancing, tilliing, chopping_tree)
    //every :10 should be .168 (drumming, gathering, 
    //every :07 is .115 (running, idle, clay gathering)
    //every :03 is .05 (hammering)
    public static float[] spriteSheet1TimeBetweenFrames = new float[]
    {
        .084f,
        .084f,
        .115f,
        .115f,
        .115f,
        .115f,
        .115f,
        .115f,
        .084f,
        .084f,
        .05f,
        .084f,
        .168f,
        .168f,
        .168f,
        1
    };

    public static bool[] spriteSheet1LoopbackDatas = new bool[]
    {
        true, //walk front
        true, //walk back
        true, //idle front
        true, //idle back
        true, //running front
        true, //running back
        false, //clay gather front
        false, //clay gather back
        true, //axeing a question
        false, //dancing
        true, //hammering
        false,//tilling
        false, //gathering
        false, //drumming with more detail
        false, //drumming less detail
        false //stool
    };

    public static int[] spriteSheet2ImageCounts = new int[]
    {
        5, //They're all 5 frames of walking animations
        5,
        5,
        5,
        5,
        5,
        5,
        5,
        5,
        5,
        5,
        5,
        5,
        5,
        5,
        5,
        5,
        5
    };

    public static string[] spriteSheet2AnimationTitles = new string[]
    {
        "clay_walking_front",
        "clay_walking_back",
        "egg_walking_front",
        "egg_walking_back",
        "matoke_walking_front",
        "matoke_walking_back",
        "Potato_walking_front",
        "potato_walking_back",
        "millet_walking_front",
        "millet_walking_back",
        "water_walking_front",
        "water_walking_back",
        "milk_walking_front",
        "milk_walking_back",
        "wood_walking_front",
        "wood_walking_back",
        "planks_walking_front",
        "planks_walking_back"
    };

    public static bool[] spriteSheet2LoopbackDatas = new bool[]
    {
        true, //all the animations on sheet 2 loop
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true,
        true
    };

    //all .084 for walking
    public static float[] spriteSheet2TimeBetweenFrames = new float[]
    {
        .084f,
        .084f,
        .084f,
        .084f,
        .084f,
        .084f,
        .084f,
        .084f,
        .084f,
        .084f,
        .084f,
        .084f,
        .084f,
        .084f,
        .084f,
        .084f,
        .084f,
        .084f
    };

[System.Serializable]
public class InfoToBuildAnimationsFromSheet
{
    public AnimationBuilder.SheetIdentity sheetIdentity;
    public Sprite[] allSpritesFromSpriteSheet;
    public string animationFilenamePrefix;
}
#endif
