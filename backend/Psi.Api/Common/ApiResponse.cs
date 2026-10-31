using System.Collections.Generic;

namespace Psi.Api.Common
{
    public class ApiResponse<T>
    {
        public int Code { get; set; } = 200;
        public string Message { get; set; } = "操作成功";
        public T? Data { get; set; }

        public static ApiResponse<T> Success(T data, string message = "操作成功")
        {
            return new ApiResponse<T> { Code = 200, Message = message, Data = data };
        }

        public static ApiResponse<T> Fail(string message = "操作失败", int code = 500)
        {
            return new ApiResponse<T> { Code = code, Message = message, Data = default };
        }
    }

    public class PageResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }
}
