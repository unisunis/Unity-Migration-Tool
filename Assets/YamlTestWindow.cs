﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using YamlDotNet.RepresentationModel;

public class YamlTestWindow : EditorWindow
{
    [MenuItem("YamlTest/TestWindow")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(YamlTestWindow));
    }

    void OnGUI()
    {
        if (GUILayout.Button("Test"))
        {
            GameObject[] gameObjects = GameObject.FindSceneObjectsOfType(typeof(GameObject)) as GameObject[];
            Array.Reverse(gameObjects);

            foreach (GameObject current in gameObjects)
            {
                Debug.LogWarning(current.name);
                foreach (var component in current.GetComponents<Component>())
                {
                    Debug.Log(component.GetType().Name);
                }
            }
        }

        if (GUILayout.Button("Yaml"))
        {
            string path = EditorUtility.OpenFilePanel("title", "", "*");
            if (path.Length != 0)
            {
                ConvertYaml(path);
            }
            else
            {
                throw new NotImplementedException("Could not get file");
            }
        }
    }

    private void ConvertYaml(string path)
    {
        GameObject[] gameObjects = GameObject.FindSceneObjectsOfType(typeof(GameObject)) as GameObject[];
        Array.Reverse(gameObjects);

        string text = File.ReadAllText(path);
        StringReader input = new StringReader(text);
        var yaml = new YamlStream();
        yaml.Load(input);

        var yamlDocuments = GetGameObjectYamlDocuments(yaml);

        if (yamlDocuments.Count != gameObjects.Length)
        {
            Debug.LogError("Different length game objects in the yaml and the scene");
        }
        else
        {
            Debug.Log("GameObjects match the yaml documents");
        }

        List<ChangeWindow.FileData> fileDatas = new List<ChangeWindow.FileData>();
        for (int i = 0; i < gameObjects.Length; i++)
        {
            var currentGameObject = gameObjects[i];
            var yamlDocument = yamlDocuments[i];


            Component[] components = currentGameObject.GetComponents<Component>();
            List<string> fileIDS = getFileIDsFromDocument(yamlDocument);
            if (fileIDS.Count != components.Length)
            {
                throw new NotImplementedException("fileIDs and components do not match");
            }

            Debug.Log("fileIDS and components matched");

            for (int j = 0; j < components.Length; j++)
            {
                Component component = components[j];
                string fileID = fileIDS[j];

                YamlDocument document = getYamlDocumentByAnchor(yaml, fileID);
                var guid = getGuidFromDocument(document);
                if (guid != null)
                {
                    fileDatas.Add(new ChangeWindow.FileData(component.GetType().Name, guid, true));
                }
            }
        }

        Debug.Log(JsonConvert.SerializeObject(fileDatas, Formatting.Indented));
    }

    private List<string> getFileIDsFromDocument(YamlDocument document)
    {
        List<string> fileIDS = new List<string>();
        YamlSequenceNode componentNode = (YamlSequenceNode) document.RootNode["GameObject"]["m_Component"];
        foreach (YamlMappingNode component in componentNode)
        {
            var componentUnwrapped = component["component"];
            var fileID = ((YamlScalarNode) componentUnwrapped["fileID"]).Value;
            Debug.Log("filedID : " + fileID);
            fileIDS.Add(fileID);
        }

        return fileIDS;
    }

    /// <summary>
    /// The Anchor is the same as the fileID
    /// </summary>
    /// <param name="yaml"></param>
    /// <param name="anchor"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private YamlDocument getYamlDocumentByAnchor(YamlStream yaml, string anchor)
    {
        foreach (YamlDocument document in yaml.Documents)
        {
            if (document.RootNode.Anchor.Equals(anchor))
            {
                return document;
            }
        }

        throw new NotImplementedException();
    }

    private string getGuidFromDocument(YamlDocument document)
    {
        try
        {
            YamlNode scriptNode = document.RootNode["MonoBehaviour"]["m_Script"]["guid"];
            return ((YamlScalarNode) scriptNode).Value;
        }
        catch (Exception e)
        {
            Debug.LogError("Could not find script for document in GetGUIDFromDocument");
            return null;
        }
    }

    private List<YamlDocument> GetGameObjectYamlDocuments(YamlStream yaml)
    {
        List<YamlDocument> yamlDocuments = new List<YamlDocument>();
        foreach (YamlDocument document in yaml.Documents)
        {
            Debug.LogWarning(document.RootNode.Anchor + ": " + document.RootNode.Tag);

            string type = "";
            foreach (KeyValuePair<YamlNode, YamlNode> entry in (YamlMappingNode) document.RootNode)
            {
                type = ((YamlScalarNode) entry.Key).Value;
//                Debug.Log("Type : " + type);
            }

            if (type.Equals("GameObject"))
            {
                yamlDocuments.Add(document);
            }
        }

        return yamlDocuments;
    }
}