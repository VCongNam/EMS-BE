using EMS.Application.Features.Posts.DTOs;
using EMS.Application.Features.Posts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Bảo mật bằng JWT
    public class PostController : ControllerBase
    {
        private readonly IPostService postService;

        public PostController(IPostService postService)
        {
            this.postService = postService;
        }

        [HttpPost]
        // TODO: Đổi [FromBody] thành [FromForm] khi làm chức năng Upload File
        public async Task<IActionResult> CreatePost([FromBody] CreatePostDto request)
        {
            try
            {
                var postId = await postService.CreatePostAsync(request);
                return StatusCode(201, new { Message = "Post created successfully!", PostId = postId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostDetail(Guid id)
        {
            try
            {
                var post = await postService.GetPostByIdAsync(id);
                return Ok(new { Message = "Success", Data = post });
            }
            catch (Exception ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        // TODO: Đổi [FromBody] thành [FromForm] khi làm chức năng Upload File
        public async Task<IActionResult> UpdatePost(Guid id, [FromBody] UpdatePostDto request)
        {
            try
            {
                await postService.UpdatePostAsync(id, request);
                return Ok(new { Message = "Post updated successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(Guid id)
        {
            try
            {
                await postService.DeletePostAsync(id);
                return Ok(new { Message = "Post deleted successfully!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> AddComment(Guid id, [FromBody] CreateCommentDto request)
        {
            try
            {
                var commentId = await postService.AddCommentAsync(id, request);
                return StatusCode(201, new { Message = "Comment added successfully!", CommentId = commentId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
