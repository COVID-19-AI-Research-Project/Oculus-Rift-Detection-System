using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Text;
using System;
using SimpleJSON;

public class OnCollisionEvent : MonoBehaviour
{
    public static string server = "https://genisysai.iotjumpway.com";
    public static string serverPath = server + "/Detection/COVID-19/CNN/API/Inference";

    public string image;
    public string imageName;

    float hitWait = 1;
    float hitClock = 0;
    bool hitOk = true;

    public Collider current = null;
    public Collider culprit = null;

    string[] materials = {
            "Covid (10)",
            "Covid (76)",
            "Covid (156)",
            "Covid (356)",
            "Covid (675)",
            "Non-Covid (1)",
            "Non-Covid (54)",
            "Non-Covid (104)",
            "Non-Covid (389)",
            "Non-Covid (582)"
    };

    void resetBlocks()
    {
        foreach (string i in materials)
        {
            GameObject dataCube = GameObject.Find(i);
            dataCube.GetComponent<MeshRenderer>().material.color = Color.white;
        }
        print("System RESET");
        current = null;
    }

    IEnumerator OnCollisionEnter(Collision collision)
    {
        if (!hitOk)
            yield break;

        ContactPoint contact = collision.contacts[0];

        culprit = contact.otherCollider;
        current = contact.thisCollider;

        if (current.name == "Reset")
        {
            resetBlocks();
            yield break;
        }

        if (culprit.name == "Beam")
        {
            print("Collision With " + current.name + " (Tag: " + current.tag + ") From " + culprit.name);

            imageName = current.name + ".png";

            image = Path.Combine(Application.streamingAssetsPath, imageName);

            List<IMultipartFormSection> form = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("file", File.ReadAllBytes(image), imageName, "image/png")
            };

            byte[] boundary = UnityWebRequest.GenerateBoundary();
            byte[] formSections = UnityWebRequest.SerializeFormSections(form, boundary);
            byte[] terminate = Encoding.UTF8.GetBytes(String.Concat("\r\n--", Encoding.UTF8.GetString(boundary), "--"));
            byte[] body = new byte[formSections.Length + terminate.Length];

            Buffer.BlockCopy(formSections, 0, body, 0, formSections.Length);
            Buffer.BlockCopy(terminate, 0, body, formSections.Length, terminate.Length);

            string contentType = String.Concat("multipart/form-data; boundary=", Encoding.UTF8.GetString(boundary));

            UnityWebRequest wr = new UnityWebRequest(serverPath, "POST");
            UploadHandler uploader = new UploadHandlerRaw(body);
            uploader.contentType = contentType;

            wr.uploadHandler = uploader;
            wr.downloadHandler = new DownloadHandlerBuffer();

            yield return wr.SendWebRequest();

            if (wr.isNetworkError || wr.isHttpError)
            {
                print(wr.error);
                hitClock = 0;
            }
            else
            {
                string json = wr.downloadHandler.text;
                JSONNode jsonData = JSON.Parse(System.Text.Encoding.UTF8.GetString(wr.downloadHandler.data));

                if (jsonData["Diagnosis"] == "Negative")
                {
                    print("Negative Classification");
                    if (imageName.Contains("Non-Covid"))
                    {
                        current.GetComponent<MeshRenderer>().material.color = Color.green;
                    }
                    else
                    {
                        current.GetComponent<MeshRenderer>().material.color = Color.cyan;
                    }
                }
                else if (jsonData["Diagnosis"] == "Positive")
                {
                    print("Postive Classification");
                    if (imageName.Contains("Non-Covid"))
                    {
                        current.GetComponent<MeshRenderer>().material.color = Color.magenta;
                    }
                    else
                    {
                        current.GetComponent<MeshRenderer>().material.color = Color.red;
                    }
                }
                hitClock = 0;
            }

        }
    }

    void OnCollisionStay(Collision collision)
    {
    }

    void OnCollisionExit(Collision collision)
    {
    }

    void Update()
    {
        hitClock += Time.deltaTime;
        if (hitClock > hitWait)
            hitOk = true;
    }
}
