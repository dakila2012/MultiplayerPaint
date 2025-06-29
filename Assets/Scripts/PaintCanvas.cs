using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class PaintCanvas : MonoBehaviourPunCallbacks
{
    [SerializeField] private RawImage canvasImage; // The UI RawImage for the canvas
    [SerializeField] private Button brushButton; // Button to select brush mode
    [SerializeField] private Button eraseButton; // Button to select eraser mode
    [SerializeField] private Button clearButton; // Button to clear the canvas
    [SerializeField] private GameObject colorPanel; // Panel with color buttons
    [SerializeField] private Button redButton; // Button to select red color
    [SerializeField] private Button blueButton; // Button to select blue color
    [SerializeField] private Button greenButton; // Button to select green color
    [SerializeField] private Slider sizeSlider; // Slider to adjust brush/eraser size
    [SerializeField] private Text playerListText; // Text to display player nicknames

    private Texture2D canvasTexture; // Texture for drawing
    private Color currentColor = Color.red; // Default brush color
    private float brushSize = 5f; // Default brush size
    private bool isBrushMode = true; // Brush or eraser mode
    private Vector2 lastMousePos; // Track last mouse position for continuous lines
    private PaintInputActions inputActions; // Input System actions
    private bool isDrawing; // Track if left mouse button is held

    void Awake()
    {
        // Initialize Input System
        inputActions = new PaintInputActions();
    }

    void OnEnable()
    {
        // Enable input actions
        inputActions.Paint.Enable();
        inputActions.Paint.Draw.performed += OnDrawPerformed;
        inputActions.Paint.Draw.canceled += OnDrawCanceled;
    }

    void OnDisable()
    {
        // Disable input actions
        inputActions.Paint.Disable();
        inputActions.Paint.Draw.performed -= OnDrawPerformed;
        inputActions.Paint.Draw.canceled -= OnDrawCanceled;
    }

    void Start()
    {
        // Initialize the canvas texture (512x512, white background)
        canvasTexture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        ClearCanvas();
        canvasImage.texture = canvasTexture;

        // Set up button listeners
        brushButton.onClick.AddListener(OnBrushButton);
        eraseButton.onClick.AddListener(OnEraseButton);
        clearButton.onClick.AddListener(OnClearButton);
        redButton.onClick.AddListener(() => SetColor(Color.red));
        blueButton.onClick.AddListener(() => SetColor(Color.blue));
        greenButton.onClick.AddListener(() => SetColor(Color.green));

        // Set up slider listener
        sizeSlider.onValueChanged.AddListener(SetBrushSize);

        // Initialize UI state
        colorPanel.SetActive(false);
        sizeSlider.value = brushSize;

        // Update player list
        UpdatePlayerList();
    }

    void Update()
    {
        if (isDrawing && PhotonNetwork.IsConnected)
        {
            // Read mouse position from Input System
            Vector2 mousePos = inputActions.Paint.MousePosition.ReadValue<Vector2>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasImage.rectTransform, mousePos, null, out var localPos);

            // Convert UI coordinates to texture coordinates
            float texX = (localPos.x + canvasImage.rectTransform.rect.width / 2) * 
                (canvasTexture.width / canvasImage.rectTransform.rect.width);
            float texY = (localPos.y + canvasImage.rectTransform.rect.height / 2) * 
                (canvasTexture.height / canvasImage.rectTransform.rect.height);

            Vector2 currentPos = new Vector2(texX, texY);

            // Draw if within texture bounds
            if (texX >= 0 && texX < canvasTexture.width && texY >= 0 && texY < canvasTexture.height)
            {
                if (!lastMousePos.Equals(Vector2.zero)) // Simulate GetMouseButtonDown
                {
                    // Send color as individual float components
                    photonView.RPC("DrawLineRPC", RpcTarget.AllBuffered, lastMousePos, currentPos, 
                        currentColor.r, currentColor.g, currentColor.b, currentColor.a, brushSize, isBrushMode);
                }
                lastMousePos = currentPos;
            }
        }
    }

    void OnDrawPerformed(InputAction.CallbackContext context)
    {
        isDrawing = true;
        lastMousePos = Vector2.zero; // Reset on new click
    }

    void OnDrawCanceled(InputAction.CallbackContext context)
    {
        isDrawing = false;
    }

    [PunRPC]
    void DrawLineRPC(Vector2 start, Vector2 end, float r, float g, float b, float a, float size, bool isBrush)
    {
        // Reconstruct Color from float components
        Color color = new Color(r, g, b, a);
        // Simple line drawing using Bresenham-like interpolation
        float distance = Vector2.Distance(start, end);
        int steps = Mathf.CeilToInt(distance / 1f);
        for (int i = 0; i <= steps; i++)
        {
            float t = steps > 0 ? i / (float)steps : 0;
            Vector2 point = Vector2.Lerp(start, end, t);
            DrawCircle(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y), color, size, isBrush);
        }
        canvasTexture.Apply();
    }

    void DrawCircle(int x, int y, Color color, float size, bool isBrush)
    {
        // Draw a filled circle for the brush/eraser
        int radius = Mathf.RoundToInt(size);
        Color drawColor = isBrush ? color : Color.white; // Eraser uses white

        for (int i = -radius; i <= radius; i++)
        {
            for (int j = -radius; j <= radius; j++)
            {
                if (i * i + j * j <= radius * radius)
                {
                    int pixelX = x + i;
                    int pixelY = y + j;
                    if (pixelX >= 0 && pixelX < canvasTexture.width && 
                        pixelY >= 0 && pixelY < canvasTexture.height)
                    {
                        canvasTexture.SetPixel(pixelX, pixelY, drawColor);
                    }
                }
            }
        }
    }

    void OnBrushButton()
    {
        isBrushMode = true;
        colorPanel.SetActive(true);
    }

    void OnEraseButton()
    {
        isBrushMode = false;
        colorPanel.SetActive(false);
    }

    void OnClearButton()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("ClearCanvasRPC", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    void ClearCanvasRPC()
    {
        ClearCanvas();
        canvasTexture.Apply();
    }

    void ClearCanvas()
    {
        // Fill texture with white
        Color[] colors = canvasTexture.GetPixels();
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        canvasTexture.SetPixels(colors);
    }

    void SetColor(Color color)
    {
        currentColor = color;
        colorPanel.SetActive(false); // Hide color panel after selecting a color
    }

    void SetBrushSize(float size)
    {
        brushSize = size;
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }

    void UpdatePlayerList()
    {
        if (playerListText != null)
        {
            string playerList = "Players:\n";
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                playerList += player.NickName + "\n";
            }
            playerListText.text = playerList;
        }
    }
}