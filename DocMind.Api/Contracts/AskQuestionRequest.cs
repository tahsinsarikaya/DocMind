namespace DocMind.Api.Contracts;

public record AskQuestionRequest(string Question, int TopK = 3);
