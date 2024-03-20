using CameraAppAPI.models;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Buffers.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Nodes;

namespace CameraAppAPI.Controllers
{



	[Route("api/[controller]")]
	[ApiController]
	public class PictureStorageController : Controller
	{
		private IConfiguration _configuration;
		public PictureStorageController(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		// Static list to store pictures across requests
		private static List<string> pictures = new List<string>();

		[Authorize]
		[HttpGet("GetPicture")]
		public ActionResult<List<string>> GetPicture()
		{
			Console.WriteLine("get pictures");

			if (pictures.Count > 0)
			{
				return Ok(pictures);
			}
			return (pictures);
		}

		[Authorize]
		[HttpPost("SavePicture")]
		public IActionResult SavePicture([FromBody] string pictureData)
		{
			Console.WriteLine("save picture");
			try
			{
				pictures.Add(pictureData);
				return Ok();
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return BadRequest();
			}
		}

		[AllowAnonymous]
		[HttpGet("sendNotification")]
		public async Task<IActionResult> SendNotification()
		{
			try
			{

				// This registration token comes from the client FCM SDKs.
				var topic = "camera-app-topic-firebase";

				// See documentation on defining a message payload.
				var message = new Message()
				{
					Topic = topic,
					Android = new()
					{
						Priority = Priority.High,
						Notification = new AndroidNotification()
						{
							Title = "this is a message only to android",
							Body = "nah, i'd notify.",
							//ImageUrl = "./Media/goblin.png",
							Color = "#666666",
							Sound = "./Media/eagle.ogg"
						}

					}
				};

				// Send a message to the devices subscribed to the provided topic.
				string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
				// Response is a message ID string.
				Console.WriteLine("Successfully sent message: " + response);
				return Ok();
			}
			catch (Exception e)
			{
				await Console.Out.WriteLineAsync("something went wrong... " + e.Message);
				return BadRequest();
			}
		}

		[AllowAnonymous]
		[HttpGet("Ping")]
		public IActionResult Ping()
		{
			Console.WriteLine("ping");

			try
			{
				return Ok("der er hul igennem!");

			}
			catch (Exception e)
			{
				Console.WriteLine($"der er ikke hul igennem... \n\n{e.Message}");
				return BadRequest("an error has occured.");
			}
		}


		[AllowAnonymous]
		[HttpPost("Login")]
		public IActionResult Login([FromBody] Login userLogin)
		{
			Console.WriteLine($"login     ${userLogin.Username} : ${userLogin.PasswordEncrypted}");

			IActionResult response = Unauthorized();

			var user = AuthenticateNewUser(userLogin);

			if (user.Username != null)
			{
				var tokenString = GenerateJSONWebToken(user);
				Console.WriteLine(tokenString);
				response = Ok(new { token = tokenString });
			}



			return response;
		}
		private string GenerateJSONWebToken(object user)
		{
			var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
			var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
				_configuration["Jwt:Audience"],
				null,
				expires: DateTime.Now.AddMinutes(15),
				signingCredentials: credentials);

			return new JwtSecurityTokenHandler().WriteToken(token);

		}


		private Login AuthenticateNewUser(Login userLogin)
		{

			Login user = null;

			if (userLogin.Username == "user" && userLogin.PasswordEncrypted == "pass")
			{
				user = new Login { Username = userLogin.Username, PasswordEncrypted = userLogin.PasswordEncrypted };
			}

			return user;
		}

	}
}
