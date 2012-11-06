/************************************************************************/
/*                                                                      */
/*   svm_struct_latent_api.c                                            */
/*                                                                      */
/*   API function definitions for Latent SVM^struct                     */
/*                                                                      */
/*   Author: Chun-Nam Yu                                                */
/*   Date: 17.Dec.08                                                    */
/*                                                                      */
/*   This software is available for non-commercial use only. It must    */
/*   not be modified and distributed without prior permission of the    */
/*   author. The author is not responsible for implications from the    */
/*   use of this software.                                              */
/*                                                                      */
/************************************************************************/

#include "stdafx.h"

#include "svm_struct_latent_api_types.h"

#include "LearningTracker.h"

#include <cstdio>
#include <cassert>
#include <cmath>

using namespace System;
using namespace System::IO;
using namespace System::Drawing;
using namespace System::Collections::Generic;
using namespace Research::GraphBasedShapePrior;
using namespace Research::GraphBasedShapePrior::Util;

using namespace cli;

const double COLOR_DIFFERENCE_CUTOFF = 0.2;

//const int MAX_ANNEALING_ITERATIONS = 3000;
//const int MAX_ANNEALING_STALL_ITERATIONS = 750;
//const int REANNEALING_INTERVAL = 750;
const int MAX_ANNEALING_ITERATIONS = 1000;
const int MAX_ANNEALING_STALL_ITERATIONS = 300;
const int REANNEALING_INTERVAL = 300;
const double ANNEALING_START_TEMPERATURE = 1.0;
const int ANNEALING_REPORT_RATE = 100;

const double EDGE_WIDTH_MUTATION_WEIGHT = 0.2;
const double EDGE_WIDTH_MUTATION_POWER = 0.1;
const double EDGE_LENGTH_MUTATION_WEIGHT = 0.25;
const double EDGE_LENGTH_MUTATION_POWER = 0.2;
const double EDGE_ANGLE_MUTATION_WEIGHT = 0.25;
const double EDGE_ANGLE_MUTATION_POWER = 0.8;
const double SHAPE_TRANSLATION_WEIGHT = 0.0;
const double SHAPE_TRANSLATION_POWER = 0.1;
const double SHAPE_SCALE_WEIGHT = 0.0;
const double SHAPE_SCALE_POWER = 0.1;

const double VERTEX_LOSS_WEIGHT = 0.001;
const double EDGE_LOSS_WEIGHT = 0.01;
const double MAX_VERTEX_LOSS_RELATIVE_DISTANCE = 0.25;
const double MAX_EDGE_LOSS_RELATIVE_DIFF = 0.1;

// Warm start
const double START_COLOR_WEIGHT = 5.567759;		
const double START_SHAPE_WEIGHT = 7.470189;
const double START_COLOR_DIFFERENCE_PAIRWISE_WEIGHT = 0.030300;

//const double START_COLOR_WEIGHT = 1.0;
//const double START_SHAPE_WEIGHT = 0.3;
//const double START_COLOR_DIFFERENCE_PAIRWISE_WEIGHT = 0.0;
const double START_CONSTANT_PAIRWISE_WEIGHT = 0.0;
const double START_SHAPE_ENERGY_WEIGHT = 0.0;

const double EDGE_LENGTH_FEATURE_SCALE = 0.002;
const double EDGE_WIDTH_FEATURE_SCALE = 0.02;

double zero_or_more(double val) {
	return val < 0 ? 0 : val;
}

double deviation_to_weight(double stddev) {
	return 1 / (2 * stddev * stddev);
}

double edge_length_deviation_to_weight(double stddev) {
	return deviation_to_weight(stddev) / EDGE_LENGTH_FEATURE_SCALE;
}

double edge_width_deviation_to_weight(double stddev) {
	return deviation_to_weight(stddev) / EDGE_WIDTH_FEATURE_SCALE;
}

double weight_to_deviation(double weight) {
	return Math::Sqrt(0.5 / (zero_or_more(weight) + 1e-10));
}

double edge_length_weight_to_deviation(double weight) {
	return weight_to_deviation(weight * EDGE_LENGTH_FEATURE_SCALE);
}

double edge_width_weight_to_deviation(double weight) {
	return weight_to_deviation(weight * EDGE_WIDTH_FEATURE_SCALE);
}

void setup_shape_model(ShapeModel ^shapeModel, STRUCTMODEL *sm) {	
	shapeModel->RootEdgeLengthDeviation = edge_length_weight_to_deviation(sm->w[FT_SHAPE_SCALE_WEIGHT]);
	
	size_t lengthDeviationFeatureIndex = FT_OTHER_SHAPE_FEATURES_START;
	size_t angleDeviationFeatureIndex = FT_OTHER_SHAPE_FEATURES_START + shapeModel->ConstrainedEdgePairs->Count;
	for (int i = 0; i < shapeModel->ConstrainedEdgePairs->Count; ++i, ++lengthDeviationFeatureIndex, ++angleDeviationFeatureIndex) {
		Tuple<int, int> ^edgePair = shapeModel->ConstrainedEdgePairs[i];
		ShapeEdgePairParams ^pairParams = shapeModel->GetMutableEdgePairParams(edgePair->Item1, edgePair->Item2);
		pairParams->LengthDiffDeviation = edge_length_weight_to_deviation(sm->w[lengthDeviationFeatureIndex]);
		pairParams->AngleDeviation = weight_to_deviation(sm->w[angleDeviationFeatureIndex]);
	}

	size_t widthDeviationFeatureIndex = FT_OTHER_SHAPE_FEATURES_START + shapeModel->ConstrainedEdgePairs->Count * 2;
	for (int i = 0; i <shapeModel->Structure->Edges->Count; ++i, ++widthDeviationFeatureIndex) {
		ShapeEdgeParams ^params = shapeModel->GetMutableEdgeParams(i);
		params->WidthToEdgeLengthRatioDeviation = edge_width_weight_to_deviation(sm->w[widthDeviationFeatureIndex]);
	}
}

void setup_segmentator(SegmentationAlgorithmBase ^segmentator, STRUCTMODEL *sm, STRUCT_LEARN_PARM *sparm) {
	segmentator->ColorUnaryTermWeight = zero_or_more(sm->w[FT_COLOR_WEIGHT]);
	segmentator->ShapeUnaryTermWeight = zero_or_more(sm->w[FT_SHAPE_WEIGHT]);
	segmentator->ColorDifferencePairwiseTermWeight = zero_or_more(sm->w[FT_COLOR_DIFFERENCE_PAIRWISE_WEIGHT]);
	segmentator->ConstantPairwiseTermWeight = zero_or_more(sm->w[FT_CONSTANT_PAIRWISE_WEIGHT]);

	segmentator->ColorDifferencePairwiseTermCutoff = COLOR_DIFFERENCE_CUTOFF;
	segmentator->ShapeEnergyWeight = 1.0;
	segmentator->ShapeModel = sparm->shape_model;

	setup_shape_model(sparm->shape_model, sm);
}

void setup_annealing(AnnealingSegmentationAlgorithm ^annealingSegmentator) {
	annealingSegmentator->SolutionFitter->MaxIterations = MAX_ANNEALING_ITERATIONS;
	annealingSegmentator->SolutionFitter->MaxStallingIterations = MAX_ANNEALING_STALL_ITERATIONS;
	annealingSegmentator->SolutionFitter->ReannealingInterval = REANNEALING_INTERVAL;
	annealingSegmentator->SolutionFitter->StartTemperature = ANNEALING_START_TEMPERATURE;
	annealingSegmentator->SolutionFitter->ReportRate = ANNEALING_REPORT_RATE;

	annealingSegmentator->ShapeMutator->EdgeWidthMutationWeight = EDGE_WIDTH_MUTATION_WEIGHT;
	annealingSegmentator->ShapeMutator->EdgeWidthMutationPower = EDGE_WIDTH_MUTATION_POWER;
	annealingSegmentator->ShapeMutator->EdgeLengthMutationWeight = EDGE_LENGTH_MUTATION_WEIGHT;
	annealingSegmentator->ShapeMutator->EdgeLengthMutationPower = EDGE_LENGTH_MUTATION_POWER;
	annealingSegmentator->ShapeMutator->EdgeAngleMutationWeight = EDGE_ANGLE_MUTATION_WEIGHT;
	annealingSegmentator->ShapeMutator->EdgeAngleMutationPower = EDGE_ANGLE_MUTATION_POWER;
	annealingSegmentator->ShapeMutator->ShapeTranslationWeight = SHAPE_TRANSLATION_WEIGHT;
	annealingSegmentator->ShapeMutator->ShapeTranslationPower = SHAPE_TRANSLATION_POWER;
	annealingSegmentator->ShapeMutator->ShapeScaleWeight = SHAPE_SCALE_WEIGHT;
	annealingSegmentator->ShapeMutator->ShapeScalePower = SHAPE_SCALE_POWER;
}

double calc_trunc_vertex_loss(Vector point1Pos, Vector point2Pos, Size imageSize) {
	//double maxDistanceSqr = MAX_VERTEX_LOSS_RELATIVE_DISTANCE * MAX_VERTEX_LOSS_RELATIVE_DISTANCE * Vector(imageSize.Width, imageSize.Height).LengthSquared;
	//double distanceSqr = (point1Pos - point2Pos).LengthSquared;
	//return MathHelper::Trunc(distanceSqr, 0, maxDistanceSqr);

	double maxDistance = MAX_VERTEX_LOSS_RELATIVE_DISTANCE * Vector(imageSize.Width, imageSize.Height).Length;
	double distance = (point1Pos - point2Pos).Length;
	return MathHelper::Trunc(distance, 0, maxDistance);
}

double calc_trunc_edge_loss(double width1, double width2, Size imageSize) {
	//double maxDiffSqr = MAX_EDGE_LOSS_RELATIVE_DIFF * MAX_EDGE_LOSS_RELATIVE_DIFF * Vector(imageSize.Width, imageSize.Height).LengthSquared;
	//double diff = width1 - width2;
	//return MathHelper::Trunc(diff * diff, 0, maxDiffSqr);

	double maxDiff = MAX_EDGE_LOSS_RELATIVE_DIFF * Vector(imageSize.Width, imageSize.Height).Length;
	double diff = Math::Abs(width1 - width2);
	return MathHelper::Trunc(diff, 0, maxDiff);
}

Tuple<double, double>^ calc_shape_loss(Shape ^shape1, Shape ^shape2, Size imageSize) {
	double vertexLoss = 0;
	for (int i = 0; i < shape1->VertexPositions->Count; ++i) {
		vertexLoss += calc_trunc_vertex_loss(shape1->VertexPositions[i], shape2->VertexPositions[i], imageSize);
	}

	double edgeLoss = 0;
	for (int i = 0; i < shape1->EdgeWidths->Count; ++i) {
		edgeLoss += calc_trunc_edge_loss(shape1->EdgeWidths[i], shape2->EdgeWidths[i], imageSize);
	}

	return gcnew Tuple<double, double>(vertexLoss * VERTEX_LOSS_WEIGHT, edgeLoss * EDGE_LOSS_WEIGHT);
}

ref class PenaltyCalcer {
private:
	Shape^ distFromShape;
	Size imageSize;

public:
	PenaltyCalcer(Shape ^shape, Size imageSize)
		: distFromShape(shape)
		, imageSize(imageSize)
	{
	}

	double Calc(Shape ^shape) {
		Tuple<double, double>^ loss = calc_shape_loss(distFromShape, shape, imageSize);
		return -(loss->Item1 + loss->Item2);
	}
};

ref class ShapeTermsCalcer {
private:
	Shape ^shape;
	ShapeModel ^shapeModel;

public:
	ShapeTermsCalcer(Shape ^shape, ShapeModel ^shapeModel) {
		this->shape = shape;
		this->shapeModel = shapeModel;
	}

	ObjectBackgroundTerm Calc(int x, int y) {
		return shapeModel->CalculatePenalties(shape, Vector(x, y));
	}
};

SAMPLE read_struct_examples(char *file, STRUCT_LEARN_PARM *sparm) {
	array<String^>^ lines = File::ReadAllLines(gcnew String(file));
	
	sparm->color_models = ObjectBackgroundColorModels::LoadFromFile(lines[0]);
	
	List<Shape^>^ shapes = gcnew List<Shape^>();
	List<Image2D<Color>^>^ images = gcnew List<Image2D<Color>^>();
	for (int i = 1; i < lines->Length; ++i) {
		array<String^>^ lineParts = lines[i]->Split('\t');
		shapes->Add(Shape::LoadFromFile(lineParts[0]));
		images->Add(Image2D::LoadFromFile(lineParts[1]));
	}

	sparm->shape_model = ShapeModel::Learn(shapes);
	Console::WriteLine("Root edge index is {0}", sparm->shape_model->RootEdgeIndex);

	SAMPLE sample;
	sample.n = shapes->Count;
	sample.examples = new EXAMPLE[sample.n];
	for (int i = 0; i < sample.n; ++i) {
		sample.examples[i].x.index = i;
		sample.examples[i].x.image = images[i];
		sample.examples[i].y.shape = shapes[i];
	}

	return sample;
}

void init_struct_model(SAMPLE sample, STRUCTMODEL *sm, STRUCT_LEARN_PARM *sparm, LEARN_PARM *lparm, KERNEL_PARM *kparm) {
	sm->sizePsi = FT_OTHER_SHAPE_FEATURES_START + sparm->shape_model->ConstrainedEdgePairs->Count * 2 + sparm->shape_model->Structure->Edges->Count - 1;
	sm->w = (double*) malloc(sizeof(double) * (sm->sizePsi + 1));
	sm->w[FT_NONE] = 0;
	sm->w[FT_COLOR_WEIGHT] = START_COLOR_WEIGHT;
	sm->w[FT_SHAPE_WEIGHT] = START_SHAPE_WEIGHT;
	sm->w[FT_COLOR_DIFFERENCE_PAIRWISE_WEIGHT] = START_COLOR_DIFFERENCE_PAIRWISE_WEIGHT;
	sm->w[FT_CONSTANT_PAIRWISE_WEIGHT] = START_CONSTANT_PAIRWISE_WEIGHT;
	
	sm->w[FT_SHAPE_SCALE_WEIGHT] = edge_length_deviation_to_weight(sparm->shape_model->RootEdgeLengthDeviation) * START_SHAPE_ENERGY_WEIGHT;

	size_t lengthDeviationFeatureIndex = FT_OTHER_SHAPE_FEATURES_START;
	size_t angleDeviationFeatureIndex = FT_OTHER_SHAPE_FEATURES_START + sparm->shape_model->ConstrainedEdgePairs->Count;
	for (int i = 0; i < sparm->shape_model->ConstrainedEdgePairs->Count; ++i, ++lengthDeviationFeatureIndex, ++angleDeviationFeatureIndex) {
		Tuple<int, int> ^edgePair = sparm->shape_model->ConstrainedEdgePairs[i];
		ShapeEdgePairParams ^pairParams = sparm->shape_model->GetEdgePairParams(edgePair->Item1, edgePair->Item2);
		sm->w[lengthDeviationFeatureIndex] = edge_length_deviation_to_weight(pairParams->LengthDiffDeviation) * START_SHAPE_ENERGY_WEIGHT;
		sm->w[angleDeviationFeatureIndex] = deviation_to_weight(pairParams->AngleDeviation) * START_SHAPE_ENERGY_WEIGHT;
	}

	size_t widthDeviationFeatureIndex = FT_OTHER_SHAPE_FEATURES_START + sparm->shape_model->ConstrainedEdgePairs->Count * 2;
	for (int i = 0; i < sparm->shape_model->Structure->Edges->Count; ++i, ++widthDeviationFeatureIndex) {
		ShapeEdgeParams ^params = sparm->shape_model->GetEdgeParams(i);
		sm->w[widthDeviationFeatureIndex] = edge_width_deviation_to_weight(params->WidthToEdgeLengthRatioDeviation) * START_SHAPE_ENERGY_WEIGHT;
	}
}

SVECTOR *psi(PATTERN x, LABEL y, LATENT_VAR h, STRUCTMODEL *sm, STRUCT_LEARN_PARM *sparm) {
	setup_shape_model(sparm->shape_model, sm);
	
	ImageSegmentator ^segmentator = gcnew ImageSegmentator(
		x.image,
		sparm->color_models,
		COLOR_DIFFERENCE_CUTOFF,
		1, 1, 1, 1);
	segmentator->SegmentImageWithShapeTerms(gcnew Func<int, int, ObjectBackgroundTerm>(gcnew ShapeTermsCalcer(y.shape, sparm->shape_model), &ShapeTermsCalcer::Calc));
	
	double colorTermSum, shapeTermSum, colorDifferencePairwiseTermSum, constantPairwiseTermSum;
	segmentator->ExtractSegmentationFeaturesForMask(h.mask, colorTermSum, shapeTermSum, colorDifferencePairwiseTermSum, constantPairwiseTermSum);

	WORD *words = (WORD*) malloc(sizeof(WORD) * (sm->sizePsi + 1));
	for (int i = 0; i <= sm->sizePsi; ++i)
		words[i].wnum = (i + 1) % (sm->sizePsi + 1);
	words[sm->sizePsi].weight = 0;

	words[FT_COLOR_WEIGHT - 1].weight = -(float)colorTermSum;
	words[FT_SHAPE_WEIGHT - 1].weight = -(float)shapeTermSum;
	words[FT_COLOR_DIFFERENCE_PAIRWISE_WEIGHT - 1].weight = -(float)colorDifferencePairwiseTermSum;
	words[FT_CONSTANT_PAIRWISE_WEIGHT - 1].weight = -(float)constantPairwiseTermSum;

	ShapeEdge rootEdge = sparm->shape_model->Structure->Edges[sparm->shape_model->RootEdgeIndex];
	words[FT_SHAPE_SCALE_WEIGHT - 1].weight = -(float)(sparm->shape_model->CalculateRootEdgeEnergyTerm(
		y.shape->VertexPositions[rootEdge.Index1], y.shape->VertexPositions[rootEdge.Index2]) / edge_length_deviation_to_weight(sparm->shape_model->RootEdgeLengthDeviation));

	size_t lengthDeviationWeightIndex = FT_OTHER_SHAPE_FEATURES_START - 1;
	size_t angleDeviationWeightIndex = FT_OTHER_SHAPE_FEATURES_START - 1 + sparm->shape_model->ConstrainedEdgePairs->Count;
	for (int i = 0; i < sparm->shape_model->ConstrainedEdgePairs->Count; ++i, ++lengthDeviationWeightIndex, ++angleDeviationWeightIndex) {
		Tuple<int, int>^ edgePair = sparm->shape_model->ConstrainedEdgePairs[i];
		Vector edgeVector1 = y.shape->GetEdgeVector(edgePair->Item1);
		Vector edgeVector2 = y.shape->GetEdgeVector(edgePair->Item2);
		ShapeEdgePairParams ^pairParams = sparm->shape_model->GetEdgePairParams(edgePair->Item1, edgePair->Item2);
		words[lengthDeviationWeightIndex].weight = -(float)(sparm->shape_model->CalculateEdgePairLengthEnergyTerm(
			edgePair->Item1, edgePair->Item2, edgeVector1, edgeVector2) / edge_length_deviation_to_weight(pairParams->LengthDiffDeviation));
		words[angleDeviationWeightIndex].weight = -(float)(sparm->shape_model->CalculateEdgePairAngleEnergyTerm(
			edgePair->Item1, edgePair->Item2, edgeVector1, edgeVector2) / deviation_to_weight(pairParams->AngleDeviation));
	}

	size_t widthDeviationWeightIndex = FT_OTHER_SHAPE_FEATURES_START - 1 + sparm->shape_model->ConstrainedEdgePairs->Count * 2;
	for (int i = 0; i < sparm->shape_model->Structure->Edges->Count; ++i, ++widthDeviationWeightIndex) {
		ShapeEdge edge = sparm->shape_model->Structure->Edges[i];
		ShapeEdgeParams ^params = sparm->shape_model->GetEdgeParams(i);
		words[widthDeviationWeightIndex].weight = -(float)(sparm->shape_model->CalculateEdgeWidthEnergyTerm(
			i, y.shape->EdgeWidths[i], y.shape->VertexPositions[edge.Index1], y.shape->VertexPositions[edge.Index2]) / edge_width_deviation_to_weight(params->WidthToEdgeLengthRatioDeviation));
	}

	SVECTOR *result = create_svector(words, NULL, 1.f);
	free(words);

	return result;
}

void find_most_violated_constraint_marginrescaling(PATTERN x, LABEL y, LABEL *ybar, LATENT_VAR *hbar, STRUCTMODEL *sm, STRUCT_LEARN_PARM *sparm) {
	AnnealingSegmentationAlgorithm ^segmentator = gcnew AnnealingSegmentationAlgorithm();
	setup_segmentator(segmentator, sm, sparm);
	setup_annealing(segmentator);
	
	segmentator->AdditionalShapePenalty = gcnew Func<Shape^, double>(gcnew PenaltyCalcer(y.shape, x.image->Size), &PenaltyCalcer::Calc);

	// Start from desired shape
	segmentator->StartShape = y.shape;
	SegmentationSolution^ desiredShapeSolution = segmentator->SegmentImage(x.image, sparm->color_models);

	// Start from mean shape
	double randomAngle = Research::GraphBasedShapePrior::Util::Random::Double(0, Math::PI * 2);
	Vector randomDirection(Math::Cos(randomAngle), -Math::Sin(randomAngle));
	segmentator->StartShape = sparm->shape_model->FitMeanShape(x.image->Width, x.image->Height, randomDirection);
	SegmentationSolution^ meanShapeSolution = segmentator->SegmentImage(x.image, sparm->color_models);
	
	SegmentationSolution ^mostViolatedConstraintSolution;
	if (desiredShapeSolution->Energy < meanShapeSolution->Energy) {
		mostViolatedConstraintSolution = desiredShapeSolution;
	} else {
		mostViolatedConstraintSolution = meanShapeSolution;
	}
	
	SimpleSegmentationAlgorithm ^trueSolutionSegmentator = gcnew SimpleSegmentationAlgorithm();
	setup_segmentator(trueSolutionSegmentator, sm, sparm);
	trueSolutionSegmentator->Shape = y.shape;
	SegmentationSolution^ trueSolution = trueSolutionSegmentator->SegmentImage(x.image, sparm->color_models);
	
	ybar->shape = mostViolatedConstraintSolution->Shape;
	hbar->mask = mostViolatedConstraintSolution->Mask;

	Tuple<double, double> ^loss = calc_shape_loss(y.shape, mostViolatedConstraintSolution->Shape, x.image->Size);
	LearningTracker::ReportLoss(x.index, loss->Item1, loss->Item2);

	LearningTracker::ReportMostViolatedConstraint(
		x.index, x.image, y.shape, mostViolatedConstraintSolution->Shape, mostViolatedConstraintSolution->Mask);
}

LATENT_VAR infer_latent_variables(PATTERN x, LABEL y, STRUCTMODEL *sm, STRUCT_LEARN_PARM *sparm) {
	SimpleSegmentationAlgorithm ^segmentator = gcnew SimpleSegmentationAlgorithm();
	setup_segmentator(segmentator, sm, sparm);
	segmentator->Shape = y.shape;
	SegmentationSolution^ solution = segmentator->SegmentImage(x.image, sparm->color_models);

	LearningTracker::ReportInferredLatentVariables(x.index, y.shape, solution->Mask);

	LATENT_VAR h;
	h.mask = solution->Mask;

	return h;
}

void init_latent_variables(SAMPLE *sample, LEARN_PARM *lparm, STRUCTMODEL *sm, STRUCT_LEARN_PARM *sparm) {
	for (int i = 0; i < sample->n; ++i) {
		sample->examples[i].h = infer_latent_variables(sample->examples[i].x, sample->examples[i].y, sm, sparm);
	}
}

double loss(PATTERN x, LABEL y, LABEL ybar, LATENT_VAR hbar, STRUCT_LEARN_PARM *sparm) {
	Tuple<double, double> ^loss = calc_shape_loss(y.shape, ybar.shape, x.image->Size);
	return loss->Item1 + loss->Item2;
}

void write_struct_model(char *file, STRUCTMODEL *sm, STRUCT_LEARN_PARM *sparm) {
	String ^baseFileName = gcnew String(file);
	
	setup_shape_model(sparm->shape_model, sm);
	sparm->shape_model->SaveToFile(String::Format("{0}.shp", baseFileName));

	StreamWriter ^writer = gcnew StreamWriter(String::Format("{0}.wgt", baseFileName));
	writer->WriteLine("COLOR_WEIGHT={0}", sm->w[FT_COLOR_WEIGHT]);
	writer->WriteLine("SHAPE_WEIGHT={0}", sm->w[FT_SHAPE_WEIGHT]);
	writer->WriteLine("COLOR_DIFFERENCE_PAIRWISE_WEIGHT={0}", sm->w[FT_COLOR_DIFFERENCE_PAIRWISE_WEIGHT]);
	writer->WriteLine("CONSTANT_PAIRWISE_WEIGHT={0}", sm->w[FT_CONSTANT_PAIRWISE_WEIGHT]);
	writer->Close();
}

void free_struct_model(STRUCTMODEL sm, STRUCT_LEARN_PARM *sparm) {
	free(sm.w);
}

void free_pattern(PATTERN x) {
}

void free_label(LABEL y) {
} 

void free_latent_var(LATENT_VAR h) {
}

void free_struct_sample(SAMPLE s) {
	for (int i=0; i < s.n; ++i) {
		free_pattern(s.examples[i].x);
		free_label(s.examples[i].y);
		free_latent_var(s.examples[i].h);
	}

	delete[] s.examples;
}

void parse_struct_parameters(STRUCT_LEARN_PARM *sparm) {
}

