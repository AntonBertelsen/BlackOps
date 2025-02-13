namespace MiniTwit.Server;

[ApiController]
[Route("sim/[controller]")]
public class SimController : ControllerBase
{
    private readonly IMessagesService _messagesService;
    private readonly IUsersService _usersService;
    private readonly ILatestService _latestService;

    public SimController(IMessagesService messagesservice, IUsersService usersService, ILatestService latestService)
    {
        _messagesService = messagesservice;
        _usersService = usersService;
        _latestService = latestService;
    }

    [HttpGet("/sim/latest")]
    public async Task<ActionResult<LatestDTO>> GetLatest()
    {
        var latestFromDB = await _latestService.GetAsync();

        if (latestFromDB == null)
        {
            return new LatestDTO() {latest = -1};
        }
        return latestFromDB;
    }

    [HttpPost("/sim/register")]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterSim user, [FromQuery(Name = "latest")] int? latestMessage)
    {
        var status = await _usersService.CreateAsync(user.ConvertToUser());

        UpdateLatest(latestMessage);

        if (status == Status.Created)
        {
            return NoContent();
        }
        else
        {
            return BadRequest();
        }
    }

    [HttpGet("/sim/msgs")]
    public async Task<ActionResult<List<Message>>> GetMessages() => await _messagesService.GetAsync();

    [HttpGet("/sim/msgs/{userID:length(24)}")]
    public async Task<ActionResult<List<Message>>> GetMessagesFromUser(string userID)
    {
        var messages = await _messagesService.GetMessageFromUserIDAsync(userID);

        if (messages is null)
        {
            return NotFound();
        }

        return messages.ToList();
    }

    [HttpPost("/sim/msgs/{username}")]
    public async Task<ActionResult> PostMessageAsUser(string username, [FromBody] MessageSim newMessage, [FromQuery(Name = "latest")] int? latestMessage)
    {
        UpdateLatest(latestMessage);

        var message = await ConvertToMessage(newMessage, username);

        if (message != null)
        {
            var status = await _messagesService.CreateAsync(message);

            if (status == Status.Created)
            {
                return NoContent();
            }
            else
            {
                return Unauthorized();
            }
        }

        return BadRequest();
    }

    [HttpPost("/sim/fllws/{username}")]
    public async Task<ActionResult> FollowUser(
        string username,
        [FromQuery(Name = "latest")] int? latestMessage,
        [FromBody] FollowSim followSim)
    {
        UpdateLatest(latestMessage);

        string? whoID = (await _usersService.GetUsernameAsync(username))?.Id;

        if (whoID == null)
        {
            return NotFound();
        }
        else
        {
            // whoID exists in DB, wanna follow/unfollow whomID.
            // Since we cant create 2 different HTTP Post endpoints,
            // We must create a single one. Therefore we merge the 2 "DTO"'s into 1.
            // Here we can check if unfollow is null, therefore follow must be set.

            // Unfollow another user
            if (followSim.unfollow != null)
            {
                var whomID = (await _usersService.GetUsernameAsync(followSim.unfollow))?.Id;

                // TODO: Can this be done smarter? less duplication
                if (whomID != null)
                {
                    await _usersService.Unfollow(whoID, whomID);
                }
                else
                {
                    return NotFound();
                }
            }
            // Follow another user
            else
            {
                var whomID = (await _usersService.GetUsernameAsync(followSim.follow!))?.Id;

                if (whomID != null)
                {
                    await _usersService.Follow(whoID, whomID);
                }
                else
                {
                    return NotFound();
                }
            }
            return NoContent();
        }
    }

    //HttpPost("/fllws/{userID:length(24)}")]

    //public async Task<ActionResult> PostUserInFollowers(string userID) =>
    //await _usersService.PostFollowerAsync(userID);

    public async Task<Message?> ConvertToMessage(MessageSim newMessage, string username)
    {
        var message = new Message();

        var authorID = (await _usersService.GetUsernameAsync(username))?.Id;

        if (authorID != null && newMessage.content != null)
        {
            message.AuthorID = authorID;
            message.AuthorName = username;
            message.Text = newMessage.content;
            message.Timestamp = DateTime.Now;
            return message;
        }
        return null;
    }

    public async void UpdateLatest(int? latest)
    {
        if (latest != null)
        {
            var status = await _latestService.UpdateAsync(new LatestDTO() {latest = (int) latest});

            // Figure out what to do here... log? error handling?
            if (status == Status.Conflict)
            {
                System.Console.WriteLine("ERROR IN LATEST UPDATEASYNC");
            }
        }
    }
}