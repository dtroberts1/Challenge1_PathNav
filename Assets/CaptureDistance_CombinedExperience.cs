﻿using System.Collections;
using Microsoft.Azure.SpatialAnchors.Unity.Examples;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


using moveTo = MoveTo;


public class CaptureDistance_CombinedExperience : MonoBehaviour
{
    bool initialGoalIsSet;
    int nbrTimes = 100;
    int anchorsReceived = 0;
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_EDITOR
        // Initialize flags
        int i = 0;
        GameObject currentFlag = GameObject.Find("flagAndPoleNbr" + i);
        if (currentFlag == null)
            Debug.Log("current is null!! unable to find future game objects or add them to list");
        else
            Debug.Log("current flag is" + currentFlag.name);
        while (currentFlag != null)
        {
            Debug.Log("adding" + currentFlag.name);
            GameObject.Find("listOfFlagsGameObj_CombinedExperience").GetComponent<ListOps>().addFlag(currentFlag);
            i++;
            currentFlag = GameObject.Find("flagAndPoleNbr" + i);
        }

        beginNavigation();
#else
        GameObject.Find("SpatialMapping_VRLab").SetActive(false);
#endif
    }

    public void beginNavigation() // Called either from Start (if just using Unity) or from UI button
    {
        Debug.Log("****************************************BEGINNING NAVIGATION*******************************************");

        if (initialGoalIsSet == false)
        {
            if ((GameObject.Find("arrow").GetComponent<moveTo>() == null) || (GameObject.Find("listOfFlagsGameObj_CombinedExperience").GetComponent<ListOps>() == null))
            {
                print("arrow or flag list are null in Capture Distance");
                return;
            }
            else if (GameObject.Find("listOfFlagsGameObj_CombinedExperience").GetComponent<ListOps>().flags.Count == 0)
            {
                print("******************Flag count is zero. Can't start navigation********************");
                return;
            }
            else // Otherwise, get first flag from the list and set it as destination
            {
                GameObject myGameObject = GameObject.Find("listOfFlagsGameObj_CombinedExperience").GetComponent<ListOps>().getNext();
                anchorsReceived++;
                GameObject.Find("arrow").GetComponent<moveTo>().setGoal(myGameObject.transform, myGameObject.name);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (nbrTimes == 200)
        {
            Debug.Log("**********************************************Camera Position is" + transform.position);
            nbrTimes = 0;
        }
        else
            nbrTimes++;

        if (MoveTo.goal) // Current goal from MoveTo
        {
            float dist;
#if !UNITY_EDITOR
            dist = Vector3.Distance(MoveTo.goal.transform.position, Camera.main.transform.position);
#elif UNITY_EDITOR
            dist = Vector3.Distance(MoveTo.goal.transform.position, transform.position);
#endif
            //print("Distance to goal: " + dist);

            if (dist < 1.5) // Made it to current goal. Update Goal
            {
                //GameObject.Find("arrow").GetComponent<moveTo>().setGoal(ListOps.getNext().transform);
                GameObject updatedGoal = GameObject.Find("listOfFlagsGameObj_CombinedExperience").GetComponent<ListOps>().getNext();
                if (updatedGoal != null)
                {
                    GameObject.Find("arrow").GetComponent<moveTo>().setGoal(updatedGoal.transform, updatedGoal.name);
                    anchorsReceived++;
                }
                else
                {
                    // First check if all destinations have been reached
                    if (anchorsReceived == GameObject.Find("AzureSpatialAnchors").GetComponent<AzureSpatialAnchors_CombinedExperience>().getNbrDestAnchors())
                    {
                        GameObject.Find("arrow").GetComponent<moveTo>().setGoal(null, "");
                        // Remove Arrow
                        GameObject.Find("arrow").transform.localScale = new Vector3((float).4, (float).4, (float).4);

                        print("All goals have been reached in Path Navigation!");
                        // Clear out Route Data and Session

                        clearRouteData();
                        pullNextItem();
                    }

                    // This is when next phase of Challenge 1 needs to be called
                }
                //moveTo.goal = ListOps.getNext().transform;
                print("Distance is " + dist + ". Updating goal!");

            }

        }
    }
    public void clearRouteData()
    {
        GameObject.Find("AzureSpatialAnchors_CombinedExperience").GetComponent<AzureSpatialAnchors_CombinedExperience>().clearRouteDataAndSession();

    }
    public async void pullNextItem()
    {
        await GameObject.Find("ExperienceController").GetComponent<AssemblyButton>().pullAndRunNextExpItem();

    }
}
