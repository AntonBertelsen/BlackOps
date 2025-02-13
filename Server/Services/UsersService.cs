namespace MiniTwit.Server;

public class UsersService : IUsersService
{
    private readonly IMongoCollection<User> _usersCollection;

    public UsersService(IOptions<MiniTwitDatabaseSettings> miniTwitDatabaseSettings, IMongoDatabase mongoDatabase)
    {
        _usersCollection = mongoDatabase.GetCollection<User>(miniTwitDatabaseSettings.Value.UsersCollectionName);
    }

    public async Task<List<User>> GetAsync() => await _usersCollection.Find(_ => true).ToListAsync();

    public async Task<List<string>> GetFollowersAsync(string id) => (await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync()).Followers.ToList();

    public async Task<User?> GetAsync(string id) => await _usersCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task<User?> GetUsernameAsync(string username) => await _usersCollection.Find(x => x.UserName == username).FirstOrDefaultAsync();

    public async Task<string> GetSalt(string username) => (await _usersCollection.Find(x => x.UserName == username).FirstOrDefaultAsync()).PasswordSalt!;

    public async Task<User?> Signin(string username, string password) => await _usersCollection.Find(x => x.UserName == username && x.Password == password).FirstOrDefaultAsync();

    public async Task<Status> CreateAsync(User newUser)
    {
        var user = await _usersCollection.Find(x => x.UserName == newUser.UserName).FirstOrDefaultAsync();

        if (user != null)
        {
            return Status.BadRequest;
        }
        else
        {
            await _usersCollection.InsertOneAsync(newUser);
            return Status.Created;
        }


    }

    public async Task UpdateAsync(string id, User updatedUser) =>
        await _usersCollection.ReplaceOneAsync(x => x.Id == id, updatedUser);

    public async Task RemoveAsync(string id) =>
        await _usersCollection.DeleteOneAsync(x => x.Id == id);

    public async Task<Status> Follow(string whoID, string whomID)
    {
        var user = (await _usersCollection.Find(x => x.Id == whoID).FirstOrDefaultAsync());
        var userWhomToFollow = (await _usersCollection.Find(x => x.Id == whomID).FirstOrDefaultAsync());


        if (user != null)
        {
            user.Follows!.Add(whomID);
            userWhomToFollow.Followers.Add(whoID);

            await UpdateAsync(user.Id!, user);
            await UpdateAsync(userWhomToFollow.Id!, userWhomToFollow);

            return Status.Success;
        }
        else
        {
            return Status.NotFound;
        }
    }

    public async Task<Status> Unfollow(string whoID, string whomID)
    {
        var user = (await _usersCollection.Find(x => x.Id == whoID).FirstOrDefaultAsync());
        var userWhomToUnfollow = (await _usersCollection.Find(x => x.Id == whomID).FirstOrDefaultAsync());

        if (user != null)
        {
            user.Follows?.Remove(whomID);
            userWhomToUnfollow.Followers.Remove(whoID);


            await UpdateAsync(user.Id!, user);
            await UpdateAsync(userWhomToUnfollow.Id!, userWhomToUnfollow);

            return Status.Success;
        }
        else
        {
            return Status.NotFound;
        }
    }
}