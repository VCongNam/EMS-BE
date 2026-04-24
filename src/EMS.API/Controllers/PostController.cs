using EMS.Application.Features.Posts.DTOs;
using EMS.Application.Features.Posts.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class PostController : ControllerBase
    {
        private readonly IPostService postService;

        public PostController(IPostService postService)
        {
            this.postService = postService;
        }


        [Authorize (Roles ="Teacher, TA")]
        [HttpPost]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDto request)
        {
                var postId = await postService.CreatePostAsync(request);
                return Ok(new { Message = "Đăng bài thành công", PostId = postId });
        }

        [Authorize(Roles = "Teacher, TA")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePost(Guid id, [FromForm] UpdatePostDto request)
        {
                await postService.UpdatePostAsync(id, request);
                return Ok(new { Message = "Cập nhật bài đăng thành công" });
        }

        [Authorize(Roles = "Teacher, TA")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePost(Guid id)
        {
                await postService.DeletePostAsync(id);
                return Ok(new { Message = "Xóa bài đăng thành công" });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPostDetail(Guid id)
        {
                var post = await postService.GetPostDetailAsync(id);
                return Ok(post);
        }

        [HttpGet("class/{classId}")]
        public async Task<IActionResult> GetPostsByClassId(Guid classId)
        {
                var posts = await postService.GetPostsByClassIdAsync(classId);
                return Ok(posts);
        }

        [HttpPost("{id}/comments")]
        public async Task<IActionResult> CreateComment(Guid id, [FromBody] CreateCommentDto request)
        {
                var commentId = await postService.CreateCommentAsync(id, request);
                return Ok(new { Message = "Bình luận thành công", CommentId = commentId });
        }

        [HttpDelete("comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
                await postService.DeleteCommentAsync(commentId);
                return Ok(new { Message = "Xóa bình luận thành công" });
        }

        [HttpGet("student/{classId}/posts")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentClassPosts(Guid classId, [FromQuery] PostFilter filter)
        {
                var result = await postService.GetClassPostsAsync(classId, filter);
                return Ok(new
                {
                    Message = "Lấy bảng tin thành công",
                    Data = result
                });
        }
    }
}
