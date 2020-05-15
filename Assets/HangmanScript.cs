using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using KModkit;

public class HangmanScript : MonoBehaviour
{


    public KMBombInfo bombInfo;
    public KMAudio audio;
    public KMSelectable leftButton;
    public KMSelectable rightButton;
    public KMSelectable submitLetter;
    public GameObject[] hangmanParts;
    public int increment = 0;

    public AudioClip chalkWriting;
    public AudioClip owwSound;

    float volume = 0.2f;

    public TextMesh LetterDisp;
    public TextMesh AnswerDisp;
    public TextMesh additionalLetters;

    public string[] alphabet = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z" };
    public string[] modernAlphabet = { "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P", "A", "S", "D", "F", "G", "H", "J", "K", "L", "Z", "X", "C", "V", "B", "N", "M"};
    public string[] vigenereAlphabet = {"B", "4", "5", "P", "R", "E", "L", "0", "A", "6", "G", "F", "D", "H", "O", "8", "C", "W", "M", "Q", "Y", "S", "J", "2", "Z", "T", "U", "9", "I", "1", "N", "3", "K", "7", "V", "X"};
    public bool[] isQueryed;
    public string answer;
    public string uncipheredanswer;
    public string currentprogress;
    public string moduleName;
    public string[] ignoredModules;
    public bool isIgnored = false;

    static int moduleIdCounter = 1;
    int moduleId = 0;
    public bool isActive = false;
    public bool isSolved = false;
    public bool inAnimation = false;
    public bool organMode = false;

    //Debug.LogFormat("[Encrypted Hangman #{0}] text", moduleId, );

    // Use this for initialization
    void Start()
    {
        ignoredModules = new string[]{ "Turn The Key", "Timing is Everything", "The Time Keeper", "Encrypted Hangman", "The Swan", "The Very Annoying Button", "Bamboozling Time Keeper", "Cookie Jars", "Divided Squares", "Random Access Memory", "Tax Returns" };
        additionalLetters.text = "";
        increment = 0;
        isIgnored = false;
        isQueryed = new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };
        while (true)
        {
            answer = bombInfo.GetSolvableModuleNames().ElementAt(UnityEngine.Random.RandomRange(0, bombInfo.GetSolvableModuleNames().Count() - 1));
            if (!bombInfo.GetSolvedModuleNames().Contains(answer))
            {
                moduleName = answer;
                break;
            }
        }
        answer = answer.Replace(" ", "");
        answer = answer.Replace("'", "");
        answer = answer.Replace(".", "");
        answer = answer.Replace("%", "");
        answer = answer.Replace("?", "");
        answer = answer.Replace("-", "");
        answer = answer.ToUpper();
        if (ignoredModules.Contains(moduleName.Trim()))
        {
            isIgnored = true;
            Debug.LogFormat("[Encrypted Hangman #{0}] -{1}- is a ignored Module.", moduleId, moduleName);
        }
        if (answer.Length > 24)
        {
            additionalLetters.text = "+" + (answer.Length - 24);
            answer = answer.Substring(0, 24);
        }
        uncipheredanswer = answer;
        Debug.LogFormat("[Encrypted Hangman #{0}] Selected module is -{1}- .", moduleId, moduleName);
        Debug.LogFormat("[Encrypted Hangman #{0}] The original message is -{1}- .", moduleId, uncipheredanswer);
        answer = encrypt(answer, UnityEngine.Random.RandomRange(0,6));
        for (int i = 0; i < hangmanParts.Length; i++)
        {
            hangmanParts[i].GetComponent<MeshRenderer>().enabled = false;
        }
        currentprogress = generateUnderscoreString(answer.Length);
        displayCurrentAnswer(currentprogress);
        Debug.LogFormat("[Encrypted Hangman #{0}] Solution is: {1}", moduleId, answer);
        if (bombInfo.GetSolvableModuleNames().Contains("Organization"))
        {
            organMode = true;
            Debug.LogFormat("[Encrypted Hangman #{0}] There is an Organization on the bomb. You are not restricted of solving this module first. Instances of {1} may be solved. ", moduleId, moduleName);
        }

        for (int i = 0; i < bombInfo.GetSolvableModuleNames().Count(); i++) {
            Debug.LogFormat("[Encrypted Hangman #{0}] {1}", moduleId, bombInfo.GetSolvableModuleNames().ElementAt(i));
        }
    }

    void Awake()
    {
        leftButton.OnInteract += delegate () { PressArrowButton(-1); return false; };
        rightButton.OnInteract += delegate () { PressArrowButton(1); return false; };
        submitLetter.OnInteract += delegate () { pressSubmitLetter(submitLetter); return false; };
    }

    // Update is called once per frame
    void Update()
    {
        if (!isIgnored && !isSolved && !inAnimation && !organMode)
        {
            for (int i = 0; i < bombInfo.GetSolvedModuleNames().Count(); i++)
            {
                if (bombInfo.GetSolvedModuleNames().ElementAt(i) == moduleName)
                {
                    Debug.LogFormat("[Encrypted Hangman #{0}] STRIKE due to: Unwanted module solved!", moduleId );
                    Strike();
                }
            }
        }
    }

    void Solve()
    {
        Debug.LogFormat("[Encrypted Hangman #{0}] MODULE SOLVED, WELL DONE!", moduleId );
        isSolved = true;
        StartCoroutine(SolveAnimation());
        GetComponent<KMBombModule>().HandlePass();

    }
    void Strike()
    {
        GetComponent<KMBombModule>().HandleStrike();
        StartCoroutine(StrikeAnimation());
    }

    IEnumerator SolveAnimation()
    {
        inAnimation = true;
        List<int> positions = new List<int>();
        for (int i = 0; i < uncipheredanswer.Length; i++)
        {
            positions.Add(i);
        }
        positions.Shuffle();
        for (int i = 0; i < uncipheredanswer.Length; i++)
        {
            string front = currentprogress.Substring(0, positions.ElementAt(i) * 2);
            string back = currentprogress.Substring(positions.ElementAt(i) * 2 + 1, currentprogress.Length - positions.ElementAt(i) * 2 - 1);
            currentprogress = front + uncipheredanswer.Substring(positions.ElementAt(i), 1) + back;
            displayCurrentAnswer(currentprogress);
            AudioSource.PlayClipAtPoint(chalkWriting, transform.position, volume);
            yield return new WaitForSeconds(0.2f);
        }
        inAnimation = false;
    }

    IEnumerator StrikeAnimation()
    {
        inAnimation = true;
        List<int> positions = new List<int>();
        for (int i = 0; i < uncipheredanswer.Length; i++)
        {
            positions.Add(i);
        }
        positions.Shuffle();
        for (int i = 0; i < uncipheredanswer.Length; i++)
        {
            string front = currentprogress.Substring(0, positions.ElementAt(i) * 2);
            string back = currentprogress.Substring(positions.ElementAt(i) * 2 + 1, currentprogress.Length - positions.ElementAt(i) * 2 - 1);
            currentprogress = front + uncipheredanswer.Substring(positions.ElementAt(i), 1) + back;
            displayCurrentAnswer(currentprogress);
            AudioSource.PlayClipAtPoint(chalkWriting, transform.position, volume);
            yield return new WaitForSeconds(0.2f);
        }
        yield return new WaitForSeconds(2f);
        inAnimation = false;
        Debug.LogFormat("[Encrypted Hangman #{0}] Resetting module.", moduleId);
        Start();
        
    }

    void PressArrowButton(int x)
    {
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, rightButton.transform);
        int i = (findAlphaPos(LetterDisp.text, alphabet) + x) % 26;
        if (i == -1)
        {
            i = 25;
        }
        LetterDisp.text = alphabet[i];
    }

    int findAlphaPos(string letter, string[] Thisalphabet)
    {                   //A0Z25
        for (int i = 0; i < Thisalphabet.Length; i++)
        {
            if (letter == Thisalphabet[i])
            {
                return i;
            }
        }
        return -1;
    }

    void displayCurrentAnswer(string currentprogress)
    {
        for (int i = 0; i < currentprogress.Length / 12; i++)
        {
            string front = currentprogress.Substring(0, 12 * (i + 1) + i);
            string back = currentprogress.Substring(12 * (i + 1) + i, currentprogress.Length - 12 * (i + 1) - i);
            currentprogress = front + "\n" + back;
        }
        AnswerDisp.text = currentprogress;
    }

    void pressSubmitLetter(KMSelectable button)
    {
        if (!inAnimation && !isSolved)
        {

            if (answer.Contains(LetterDisp.text) && !isQueryed[findAlphaPos(LetterDisp.text, alphabet)])              //maybe seperate
            {
                
                AudioSource.PlayClipAtPoint(chalkWriting, transform.position, volume);
                makeLetterVisible(LetterDisp.text);
                isQueryed[findAlphaPos(LetterDisp.text, alphabet)] = true;
                displayCurrentAnswer(currentprogress);
                Debug.LogFormat("[Encrypted Hangman #{0}] Successfully queried {1}. Message now is: {2}", moduleId, LetterDisp.text, currentprogress);
                if (!currentprogress.Contains("_"))
                {
                    Solve();
                }
            }
            else
            {
                Debug.LogFormat("[Encrypted Hangman #{0}] Unsuccessfully queried {1}. You queried {2} wrong letters.", moduleId, LetterDisp.text, increment + 1);
                if (increment >= hangmanParts.Length - 1)
                {
                    hangmanParts[increment].GetComponent<MeshRenderer>().enabled = true;
                    Debug.LogFormat("[Encrypted Hangman #{0}] STRIKE due to: To many wrong letters queried.", moduleId );
                    Strike();
                }
                else
                {
                    AudioSource.PlayClipAtPoint(owwSound, transform.position, volume);
                    hangmanParts[increment].GetComponent<MeshRenderer>().enabled = true;
                    increment++;
                }
            }
        }
    }

    void makeLetterVisible(string letter)
    {
        char[] charAnswer = answer.ToCharArray();
        char[] charCurrentAnswer = currentprogress.ToCharArray();

        for (int i = 0; i < charAnswer.Length; i++)
        {
            if (letter.ToCharArray()[0] == charAnswer[i])
            {
                charCurrentAnswer[i * 2] = letter.ToCharArray()[0];
            }

        }
        currentprogress = new string(charCurrentAnswer);
    }

    string generateUnderscoreString(int length)
    {
        string temp = "_";
        for (int i = 0; i < length - 1; i++)
        {
            temp = temp + " _";
        }
        return temp;
    }

    public string encrypt(string text, int method)
    {
        switch (method)
        {
            case 0:
                Debug.LogFormat("[Encrypted Hangman #{0}] Chosen encryption is Caesar Cipher with key {1} .", moduleId, bombInfo.GetSerialNumberNumbers().ElementAt(0));
                return caesarCipher(text, bombInfo.GetSerialNumberNumbers().ElementAt(0));
            case 1:
                string temp = bombInfo.GetSerialNumber();
                string convertedSN = "";
                for (int i = 0; i < temp.Length; i++) {
                    char tempchar = temp.Substring(i, 1).ToCharArray()[0];
                    if (tempchar >= '0' && tempchar <= '9')
                    {
                        convertedSN = convertedSN + alphabet[tempchar - '0'];
                    }
                    else {
                        convertedSN = convertedSN + tempchar;
                    }
                }
                convertedSN.ToUpper();
                Debug.LogFormat("[Encrypted Hangman #{0}] Chosen encryption is Playfair Cipher with key {1} .", moduleId, convertedSN);
                return playfairCipher(text, convertedSN);
            case 2:
                Debug.LogFormat("[Encrypted Hangman #{0}] Chosen encryption is Rot13 Cipher.", moduleId);
                return rot13Cipher(text);
            case 3:
                Debug.LogFormat("[Encrypted Hangman #{0}] Chosen encryption is Atbash Cipher.", moduleId);
                return atbashCipher(text);
            case 4:
                Debug.LogFormat("[Encrypted Hangman #{0}] Chosen encryption is Affine Cipher with key {1} .", moduleId, bombInfo.GetSerialNumberNumbers().ElementAt(bombInfo.GetSerialNumberNumbers().Count() - 1)*2 + 1);
                return affineCipher(text, bombInfo.GetSerialNumberNumbers().ElementAt(bombInfo.GetSerialNumberNumbers().Count()-1));
            case 5:
                Debug.LogFormat("[Encrypted Hangman #{0}] Chosen encryption is Modern Cipher with key {1} .", moduleId, bombInfo.GetSerialNumberNumbers().Sum());
                return modernCipher(text, bombInfo.GetSerialNumberNumbers().Sum());
            case 6:
                return ViginereCipher(text, bombInfo.GetSerialNumber().ToString());
            default: return text;
        }
    }

    public string caesarCipher(string text, int key)
    {
        string temp = "";
        for (int i = 0; i < text.Length; i++)
        {
            temp = temp + alphabet[(findAlphaPos(text.Substring(i,1), alphabet) + key) % 26];

        }
        return temp;
    }

    public string modernCipher(string text, int key) {
        string temp = "";
        for (int i = 0; i < text.Length; i++)
        {
            temp = temp + modernAlphabet[(findAlphaPos(text.Substring(i, 1), modernAlphabet) + key) % 26];

        }
        return temp;
    }

    public string affineCipher(string text, int key)
    {
        key = key * 2 + 1;
        string temp = "";
        for (int i = 0; i < text.Length; i++)
        {
            temp = temp + alphabet[((findAlphaPos(text.Substring(i, 1), alphabet) + 1) * key - 1) % 26];
        }
        return temp;

    }

    public string ViginereCipher(string text, string key) {
        for (int i = 0; i < text.Length / key.Length + 1; i++) {
            key = key + key;
        }
        key = key.Substring(0, text.Length);
        Debug.LogFormat("[Encrypted Hangman #{0}] Chosen encryption is Vigenère Cipher with key {1} .", moduleId, key);
        string temp = "";
        for (int i = 0; i < text.Length; i++) {
            temp = temp + vigenereAlphabet[(findAlphaPos(text.Substring(i, 1), vigenereAlphabet) + findAlphaPos(key.Substring(i, 1), vigenereAlphabet))%36];
        }
        temp = temp.Replace("0", "A");
        temp = temp.Replace("1", "B");
        temp = temp.Replace("2", "C");
        temp = temp.Replace("3", "D");
        temp = temp.Replace("4", "E");
        temp = temp.Replace("5", "F");
        temp = temp.Replace("6", "G");
        temp = temp.Replace("7", "H");
        temp = temp.Replace("8", "I");
        temp = temp.Replace("9", "J");
        return temp;

    }

    //[0,0][1,0][2,0][3,0][4,0]      [col|row]
    //[0,1][1,1][2,1][3,1][4,1]
    //[0,2][1,2][2,2][3,2][4,3]  
    //[0,3][1,3][2,3][3,3][4,3]
    //[0,4][1,4][2,4][3,4][4,4]

    public string playfairCipher(string text, string key)
    {
        
        key = key.ToUpper();
        text = text.Replace("J", "I");
        key = key.Replace("J", "I");
        string[,] cipherTable = new string[5, 5];
        List<string> newKey = new List<string>();
        string alpha = "ABCDEFGHIKLMNOPQRSTUVWXYZ";
        for (int i = 0; i < key.Length; i++)
        {
            if (!newKey.Contains(key.Substring(i, 1)))
            {
                newKey.Add(key.Substring(i, 1));
                alpha = alpha.Replace(key.Substring(i, 1), string.Empty);
            }
        }
        key = "";
        for (int i = 0; i < newKey.Count(); i++)
        {
            key = key + newKey[i];
        }
        key = key + alpha;
        for (int i = 0; i < key.Length; i++)
        {
            cipherTable[i % 5, i / 5] = key.Substring(i, 1);
        }
        Debug.LogFormat("[Encrypted Hangman #{0}] The keysquare is:", moduleId);
        Debug.LogFormat("[Encrypted Hangman #{0}] {1} {2} {3} {4} {5}", moduleId, cipherTable[0, 0], cipherTable[1, 0], cipherTable[2, 0], cipherTable[3, 0], cipherTable[4, 0]);
        Debug.LogFormat("[Encrypted Hangman #{0}] {1} {2} {3} {4} {5}", moduleId, cipherTable[0, 1], cipherTable[1, 1], cipherTable[2, 1], cipherTable[3, 1], cipherTable[4, 1]);
        Debug.LogFormat("[Encrypted Hangman #{0}] {1} {2} {3} {4} {5}", moduleId, cipherTable[0, 2], cipherTable[1, 2], cipherTable[2, 2], cipherTable[3, 2], cipherTable[4, 2]);
        Debug.LogFormat("[Encrypted Hangman #{0}] {1} {2} {3} {4} {5}", moduleId, cipherTable[0, 3], cipherTable[1, 3], cipherTable[2, 3], cipherTable[3, 3], cipherTable[4, 3]);
        Debug.LogFormat("[Encrypted Hangman #{0}] {1} {2} {3} {4} {5}", moduleId, cipherTable[0, 4], cipherTable[1, 4], cipherTable[2, 4], cipherTable[3, 4], cipherTable[4, 4]);
        bool addedX = false;
        if (text.Length % 2 == 1)
        {
            text = text + "X";
            addedX = true;
            
        }
        Debug.LogFormat("[Encrypted Hangman #{0}] The message for playfair is {1}", moduleId, text);
        int x1 = 0;
        int x2 = 0;
        int y1 = 0;
        int y2 = 0;
        string cipheredtext = "";

        for (int i = 0; i < text.Length / 2; i++)
        {
            int[,] temp = findLetterinPlayfairSquare(text.Substring(i * 2, 1), cipherTable);
            x1 = temp[0, 0];
            y1 = temp[0, 1];
            temp = findLetterinPlayfairSquare(text.Substring(i * 2 + 1, 1), cipherTable);
            x2 = temp[0, 0];
            y2 = temp[0, 1];

            if (x1 == x2 && y1 == y2)
            {                                                 //if double --> change to XZ --> if ZZ --> ZX
                temp = findLetterinPlayfairSquare("Z", cipherTable);
                x2 = temp[0, 0];
                y2 = temp[0, 1];
                if (x1 == x2 && y1 == y2)
                {
                    temp = findLetterinPlayfairSquare("X", cipherTable);
                    x2 = temp[0, 0];
                    y2 = temp[0, 1];
                }
            }
            if (x1 == x2)
            {
                cipheredtext = cipheredtext + cipherTable[x1, (y1 + 1) % 5];
                cipheredtext = cipheredtext + cipherTable[x2, (y2 + 1) % 5];
            }
            else if (y1 == y2)
            {
                cipheredtext = cipheredtext + cipherTable[(x1 + 1) % 5, y1];
                cipheredtext = cipheredtext + cipherTable[(x2 + 1) % 5, y2];
            }
            else
            {
                cipheredtext = cipheredtext + cipherTable[x2, y1];
                cipheredtext = cipheredtext + cipherTable[x1, y2];
            }
        }
        if (addedX)
        {
            return cipheredtext.Substring(0, cipheredtext.Length - 1);
        }
        else {
            return cipheredtext;
        }
    }

    public int[,] findLetterinPlayfairSquare(string letter, string[,] square)
    {
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (letter == square[i, j])
                {
                    return new int[,] { { i, j } };
                }
            }
        }
        return null;
    }

    public string rot13Cipher(string text)
    {
        string temp = "";
        for (int i = 0; i < text.Length; i++)
        {
            temp = temp + alphabet[(findAlphaPos(text.Substring(i, 1), alphabet) + 13) % 26];
        }
        return temp;
    }

    public string atbashCipher(string text)
    {
        string temp = "";
        for (int i = 0; i < text.Length; i++)
        {
            temp = temp + alphabet[25 - findAlphaPos(text.Substring(i, 1), alphabet)];
        }
        return temp;
    }


    //-------------------------------------------------- TP SUPPORT ----------------------------------------------------------------------------

    private readonly string TwitchHelpMessage = "Use !{0} select ABCDEFG to enter or query those letters. WARNING: The command will not stop upon querying a wrong letter! ";
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToUpper();
        if (Regex.IsMatch(command, @"^SELECT [A-Z]+$"))
        {
            command = command.Substring(6).Trim();
            answer = answer.Replace(" ", "");
            char[] temp = command.ToCharArray();
            for (int i = 0; i < temp.Length; i++)
            {
                int times = (findAlphaPos(temp[i].ToString(), alphabet) - findAlphaPos(LetterDisp.text, alphabet) + 26) % 26;
                for (int j = 0; j < times; j++)
                {
                    rightButton.OnInteract();
                    yield return new WaitForSeconds(0.05f);
                }
                submitLetter.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            yield break;
        }
        else { yield return null; }
    }
}
