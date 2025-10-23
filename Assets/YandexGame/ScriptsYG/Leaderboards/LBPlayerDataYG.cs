using System;
using UnityEngine;
using UnityEngine.UI;
#if YG_TEXT_MESH_PRO
using TMPro;
#endif

namespace YG
{
    [HelpURL("https://www.notion.so/PluginYG-d457b23eee604b7aa6076116aab647ed#7f075606f6c24091926fa3ad7ab59d10")]
    public class LBPlayerDataYG : MonoBehaviour
    {
        public ImageLoadYG imageLoad;
        public MonoBehaviour[] topPlayerActivityComponents = new MonoBehaviour[0];
        public Image thisPlayerActivityComponents;

#if YG_TEXT_MESH_PRO
        [Serializable]
        public struct TextMP
        {
            public TextMeshProUGUI rank, name, score;
        }
        public TextMP textMP;
#endif

        public class Data
        {
            public string rank;
            public string name;
            public string score;
            public string photoUrl;
            public bool inTop;
            public bool thisPlayer;
            public Sprite photoSprite;
        }

        [HideInInspector]
        public Data data = new Data();


        [ContextMenu(nameof(UpdateEntries))]
        public void UpdateEntries()
        {
#if YG_TEXT_MESH_PRO
            if (textMP.rank && data.rank != null) textMP.rank.text = data.rank.ToString();
            if (textMP.name && data.name != null) textMP.name.text = data.name;
            if (textMP.score && data.score != null) textMP.score.text = data.score.ToString();
#endif
            if (imageLoad)
            {
                if (data.photoSprite)
                {
                    imageLoad.PutSprite(data.photoSprite);
                }
                else if (data.photoUrl == null)
                {
                    imageLoad.ClearImage();
                }
                else
                {
                    imageLoad.Load(data.photoUrl);
                }
            }

            // if (topPlayerActivityComponents.Length > 0)
            // {
            //     if (data.inTop)
            //     {
            //         ActivityMomoObjects(topPlayerActivityComponents, true);
            //     }
            //     else
            //     {
            //         ActivityMomoObjects(topPlayerActivityComponents, false);
            //     }
            // }

            if (thisPlayerActivityComponents != null)
            {
                if (data.thisPlayer)
                {

                    thisPlayerActivityComponents.color = new Color(0.1076896f, 0.4150943f, 0.3219414f);
                    // ActivityMomoObjects(thisPlayerActivityComponents, true);
                }
                else
                {
                    thisPlayerActivityComponents.color = new Color(0.129717f, 0.5188679f, 0.4010516f);

                    // ActivityMomoObjects(thisPlayerActivityComponents, false);
                }
            }
        }
    }
}