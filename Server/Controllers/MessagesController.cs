namespace MiniTwit.Server;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IMessagesService _messagesService;

    public MessagesController(IMessagesService messagesService) => _messagesService = messagesService;

    [HttpGet]
    public async Task<List<Message>> Get() => await _messagesService.GetAsync();

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<Message>> Get(string id)
    {
        var message = await _messagesService.GetAsync(id);

        if (message is null)
        {
            return NotFound();
        }

        return Ok(message);
    }

    [HttpGet("followingMessages/{userID}")]
    public async Task<ActionResult<List<Message>>> GetFollowingMessages(string userID)
    {
        var messages = await _messagesService.GetMessagesFromFollowing(userID);

        if (messages is null)
        {
            return NotFound();
        }

        return Ok(messages);
    }

    [HttpGet("userID/{userID:length(24)}")]
    public async Task<ActionResult<List<Message>>> GetFromUserID(string userID)
    {
        var message = await _messagesService.GetMessageFromUserIDAsync(userID);

        if (message is null)
        {
            return NotFound();
        }

        return Ok(message);
    }

    [HttpPost]
    public async Task<IActionResult> Post(Message newMessage)
    {
        newMessage.Timestamp = DateTime.Now;
        var status = await _messagesService.CreateAsync(newMessage);

        if (status == Status.Created)
        {
            return CreatedAtAction(nameof(Get), new { id = newMessage.Id }, newMessage);

        }
        else
        {
            return BadRequest();
        }
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> Update(string id, Message updateMessage)
    {
        var message = await _messagesService.GetAsync(id);

        if (message is null)
        {
            return NotFound();
        }

        updateMessage.Id = message.Id;

        await _messagesService.UpdateAsync(id, updateMessage);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id)
    {
        var message = await _messagesService.GetAsync(id);

        if (message is null)
        {
            return NotFound();
        }

        await _messagesService.RemoveAsync(message.Id!);

        return NoContent();
    }
}